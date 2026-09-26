param([string]$AppPath = "$PSScriptRoot\..\src\SBotManager\bin\Debug\net10.0-windows\SBotManager.exe")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class NativeDialogTest {
  [DllImport("user32.dll")] public static extern IntPtr GetDlgItem(IntPtr dialog, int id);
  [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr SendMessageTimeout(IntPtr window, uint msg, IntPtr w, IntPtr l, uint flags, uint timeout, out IntPtr result);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern bool SetWindowText(IntPtr window, string text);
  [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out RECT rect);
  public struct RECT { public int Left, Top, Right, Bottom; }
  private delegate bool EnumCallback(IntPtr h, IntPtr p);
  [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumCallback callback, IntPtr p);
  [DllImport("user32.dll")] private static extern int GetDlgCtrlID(IntPtr window);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int count);
  public static IntPtr FileNameEditor(IntPtr dialog) {
    // The file dialog's file-name field is control ID 1148 (cmb13). Grab it by ID first;
    // enumerating the first Edit child can return the toolbar search box instead.
    IntPtr direct = GetDlgItem(dialog, 1148);
    if (direct != IntPtr.Zero) { return direct; }
    IntPtr found=IntPtr.Zero;
    EnumChildWindows(dialog, (h,p) => { var cls=new StringBuilder(256); GetClassName(h,cls,256); if(cls.ToString()=="Edit") { Console.WriteLine("Native filename candidate ID: "+GetDlgCtrlID(h)); found=h; return false; } return true; },IntPtr.Zero);
    return found;
  }
  public static string EditText(IntPtr window) { var text=new StringBuilder(1024); GetWindowText(window,text,1024); return text.ToString(); }
}
'@
$AppPath = [IO.Path]::GetFullPath($AppPath)
$data = [IO.Path]::GetFullPath("$PSScriptRoot\..\artifacts\ui-$([Guid]::NewGuid().ToString('N'))")
[void][IO.Directory]::CreateDirectory($data)
$accounts = @('AccountA','AccountB','AccountC','AccountD') | ForEach-Object { @{ Id=[Guid]::NewGuid().ToString(); Name=$_; ExecutablePath=''; RecoveryEnabled=$false } }
[IO.File]::WriteAllText("$data\settings.json", (@{ Version=1; Accounts=@($accounts); CloseToTray=$true } | ConvertTo-Json -Depth 5))
$script:checks = 0

$process = Start-Process -FilePath $AppPath -ArgumentList @('--data-dir', "`"$data`"") -PassThru
for ($i=0; $i -lt 50; $i++) { Start-Sleep -Milliseconds 200; $process.Refresh(); if ($process.MainWindowHandle -ne 0) { break } }
$script:window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
function Get-Window { $script:window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle) }
function Find-Id($root, [string]$id) {
 $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$id)
 return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)
}
function Check([bool]$ok,[string]$description) {
 if (!$ok) { throw "FAIL: $description" }; $script:checks++; Write-Output "PASS: $description"
}
function Invoke-Id($root, [string]$id) {
 for ($i=0; $i -lt 40; $i++) {
  try { $element=Find-Id $root $id } catch { $element=$null }
  if ($null -ne $element) { $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 150; return }
  Start-Sleep -Milliseconds 100
 }
 throw "Control not found or uninvokable: $id"
}
function Toggle-Id($root, [string]$id) { (Find-Id $root $id).GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle(); Start-Sleep -Milliseconds 150 }
function Set-Value($element,[string]$value) { $element.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($value) }
function Dialog-Click($dialog,[int]$id) {
 $button=[NativeDialogTest]::GetDlgItem([IntPtr]$dialog.Current.NativeWindowHandle,$id)
 if ($button -eq [IntPtr]::Zero) { throw "Native dialog button $id not found." }
 $result=[IntPtr]::Zero
 if ([NativeDialogTest]::SendMessageTimeout($button,0xF5,[IntPtr]::Zero,[IntPtr]::Zero,2,1500,[ref]$result) -eq [IntPtr]::Zero) { throw "Native dialog button $id timed out." }
 Start-Sleep -Milliseconds 200
}
function Find-Dialog([string]$title) {
  $condition=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Window)
  # Owned WPF modal windows are not consistently exposed as descendants of
  # their owner after an application restart. Search the desktop tree and
  # retain only windows belonging to this isolated test process.
  $dialogs=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Descendants,$condition)
  foreach ($dialog in $dialogs) { if ($dialog.Current.ProcessId -eq $script:window.Current.ProcessId -and $dialog.Current.Name -eq $title) { return $dialog } }
  return $null
}
function Dialog([string]$title) {
 for ($i=0; $i -lt 60; $i++) { $dialog=Find-Dialog $title; if ($null -ne $dialog) { return $dialog }; Start-Sleep -Milliseconds 100 }
 throw "Dialog did not appear: $title"
}
function Wait-DialogGone([string]$title) {
 for ($i=0; $i -lt 40; $i++) { if ($null -eq (Find-Dialog $title)) { return }; Start-Sleep -Milliseconds 150 }
 throw "Dialog did not close: $title"
}

# First-run reality: a brand-new data directory has no settings file and no seeded accounts.
$fresh = [IO.Path]::GetFullPath("$PSScriptRoot\..\artifacts\fresh-$([Guid]::NewGuid().ToString('N'))")
[void][IO.Directory]::CreateDirectory($fresh)
$freshProcess = Start-Process -FilePath $AppPath -ArgumentList @('--data-dir', "`"$fresh`"") -PassThru
for ($i=0; $i -lt 50; $i++) { Start-Sleep -Milliseconds 200; $freshProcess.Refresh(); if ($freshProcess.MainWindowHandle -ne 0) { break } }
$freshWindow = [System.Windows.Automation.AutomationElement]::FromHandle($freshProcess.MainWindowHandle)
Start-Sleep -Milliseconds 1200
try {
 Check ($null -ne (Find-Id $freshWindow 'AddAccount')) 'First run shows the add-account card'
 Check ((Find-Id $freshWindow 'Summary').Current.Name -like '0/10*') 'First run reports an empty account list'
  Check ($null -eq (Find-Id $freshWindow 'ConfigureAccountA')) 'First run contains no pre-seeded characters'
} finally {
 Stop-Process -Id $freshProcess.Id -ErrorAction SilentlyContinue
}

try {
  Start-Sleep -Milliseconds 1200
  Check ($null -ne (Find-Id $script:window 'OpenAbout')) 'Global About and support entry is available'
  Invoke-Id $script:window 'OpenAbout'; $about=Dialog 'About i1988 - sBot manager'
  Check ($null -ne (Find-Id $about 'OpenKofi') -and $null -ne (Find-Id $about 'OpenInstaPay') -and $null -ne (Find-Id $about 'ContactSupport')) 'About and support exposes Ko-fi, InstaPay, and contact actions'
  Invoke-Id $about 'CloseAbout'; Wait-DialogGone 'About i1988 - sBot manager'
  Check ((Find-Id $script:window 'OpenBots').Current.BoundingRectangle.Top -lt (Find-Id $script:window 'StartTrainingAll').Current.BoundingRectangle.Top) 'Launch all appears above bulk Start training'
  Check ((Find-Id $script:window 'OpenBotsSequential').Current.BoundingRectangle.Top -gt (Find-Id $script:window 'OpenBots').Current.BoundingRectangle.Bottom -and (Find-Id $script:window 'OpenBotsSequential').Current.BoundingRectangle.Top -lt (Find-Id $script:window 'StartTrainingAll').Current.BoundingRectangle.Top) 'Launch 1-1 is grouped below Launch all and above the divider'
   Check ((Find-Id $script:window 'StartTrainingAll').Current.BoundingRectangle.Left -lt (Find-Id $script:window 'CharacterColumn').Current.BoundingRectangle.Left) 'Bulk training controls are in left sidebar'
    Check ((Find-Id $script:window 'ShowAllBots').Current.BoundingRectangle.Top -gt (Find-Id $script:window 'StopTrainingAll').Current.BoundingRectangle.Top) 'Show all bots appears below the training section'
    Check ((Find-Id $script:window 'HideAllBots').Current.BoundingRectangle.Top -gt (Find-Id $script:window 'ShowAllBots').Current.BoundingRectangle.Top) 'Hide all bots appears below Show all bots'
    Check ((Find-Id $script:window 'ShowAllClients').Current.BoundingRectangle.Top -gt (Find-Id $script:window 'HideAllBots').Current.BoundingRectangle.Top) 'Show all clients appears below the bot actions'
    Check ((Find-Id $script:window 'HideAllClients').Current.BoundingRectangle.Top -gt (Find-Id $script:window 'ShowAllClients').Current.BoundingRectangle.Top -and (Find-Id $script:window 'GoClientlessAll').Current.BoundingRectangle.Top -gt (Find-Id $script:window 'HideAllClients').Current.BoundingRectangle.Top -and (Find-Id $script:window 'TerminateAll').Current.BoundingRectangle.Top -gt (Find-Id $script:window 'GoClientlessAll').Current.BoundingRectangle.Top) 'Go clientless all appears above the final Terminate all section'
  Check ((Find-Id $script:window 'ActivityPanel').Current.BoundingRectangle.Left -gt (Find-Id $script:window 'CharacterColumn').Current.BoundingRectangle.Right) 'Logs beside character column'
  $characterColumn=Find-Id $script:window 'CharacterColumn'
  $scrollCondition=New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ScrollBar)
  Check ($null -eq $characterColumn.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $scrollCondition)) 'Character list remains scrollable without a visible scrollbar'
    Check (!(Find-Id $script:window 'BotActionAccountA').Current.IsEnabled) 'Unconfigured executable cannot launch'
   Check ($null -eq (Find-Id $script:window 'StartTrainingAccountA')) 'Manual Start bot control removed'
   Check (!(Find-Id $script:window 'ShowHideAccountA').Current.IsEnabled) 'Window command disabled until bot verified running'
   Check (!(Find-Id $script:window 'ShowHideClientAccountA').Current.IsEnabled) 'Client window command disabled until a related client is verified'
   Check ($null -eq (Find-Id $script:window 'StartGameAccountA')) 'Manual Start game control removed'
  Check ($null -eq (Find-Id $script:window 'ActivityAccountFilter') -and $null -eq (Find-Id $script:window 'ActivitySeverityFilter')) 'Log filters removed'
 Check ($null -ne (Find-Id $script:window 'AddAccount')) 'Add-account card visible below capacity'
   $column=[Math]::Round((Find-Id $script:window 'BotActionAccountA').Current.BoundingRectangle.Left,1)
   Check ((Find-Id $script:window 'BotActionAccountA').Current.BoundingRectangle.Top -gt (Find-Id $script:window 'ShowHideClientAccountA').Current.BoundingRectangle.Top) 'Per-bot Launch action appears at the bottom of the card controls'
   Check ([Math]::Round((Find-Id $script:window 'BotActionAccountB').Current.BoundingRectangle.Top,1) -eq [Math]::Round((Find-Id $script:window 'BotActionAccountA').Current.BoundingRectangle.Top,1) -and (Find-Id $script:window 'BotActionAccountB').Current.BoundingRectangle.Left -gt (Find-Id $script:window 'BotActionAccountA').Current.BoundingRectangle.Left) 'Character grid lays out two columns'
   Check ([Math]::Round((Find-Id $script:window 'BotActionAccountC').Current.BoundingRectangle.Left,1) -eq $column -and (Find-Id $script:window 'BotActionAccountC').Current.BoundingRectangle.Top -gt (Find-Id $script:window 'BotActionAccountA').Current.BoundingRectangle.Top) 'Character grid lays out more than one row'

 # Test actual picker selection without opening the chosen executable.
  Invoke-Id $script:window 'ConfigureAccountA'; $settings=Dialog 'Character settings'
 Invoke-Id $settings 'BrowseExecutable'; $picker=Dialog "Choose the character's sBot executable"
 Start-Sleep -Milliseconds 500
 Dialog-Click $picker 2; Wait-DialogGone "Choose the character's sBot executable"
 Get-Window; $settings=Dialog 'Character settings'
  Check ((Find-Id $settings 'ExecutablePath').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value -eq '' -and !(Find-Id $settings 'ExecutablePath').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.IsReadOnly) 'Executable path accepts pasted or typed local paths; picker cancellation preserves the original value'
Invoke-Id $settings 'BrowseExecutable'; $picker=Dialog "Choose the character's sBot executable"
  Start-Sleep -Milliseconds 400
  $edit=[NativeDialogTest]::FileNameEditor([IntPtr]$picker.Current.NativeWindowHandle)
  if ($edit -eq [IntPtr]::Zero) { throw 'Native file-name editor missing.' }
  $fixture=[IO.Path]::GetFullPath("$PSScriptRoot\BotFixture\bin\Debug\net10.0-windows\BotFixture.exe")
  function Select-FixturePath($picker, $edit, $fixture) {
   # Focus the file-name field precisely (its own window rect), pre-fill it, retype the whole
   # path, then press Enter. Shell menus remember a previously selected entry, so typing the
   # full path is what makes the result deterministic.
   [NativeDialogTest]::SetForegroundWindow([IntPtr]$picker.Current.NativeWindowHandle) | Out-Null
   Start-Sleep -Milliseconds 250
   $r=[NativeDialogTest+RECT]::new()
   [void][NativeDialogTest]::GetWindowRect($edit,[ref]$r)
   $cx=[int](($r.Left + $r.Right) / 2); $cy=[int](($r.Top + $r.Bottom) / 2)
   [NativeDialogTest]::SetCursorPos($cx,$cy) | Out-Null
   Start-Sleep -Milliseconds 120
   [NativeDialogTest]::mouse_event(0x0002,0,0,0,[UIntPtr]::Zero)
   [NativeDialogTest]::mouse_event(0x0004,0,0,0,[UIntPtr]::Zero)
   Start-Sleep -Milliseconds 250
   if (![NativeDialogTest]::SetWindowText($edit,$fixture)) { throw 'Could not set test fixture filename.' }
   Start-Sleep -Milliseconds 200
   [System.Windows.Forms.SendKeys]::SendWait('^a'); Start-Sleep -Milliseconds 150
   [System.Windows.Forms.SendKeys]::SendWait($fixture); Start-Sleep -Milliseconds 300
   Check ([NativeDialogTest]::EditText($edit) -eq $fixture) 'Native file-name editor actually holds the chosen path'
   [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
  }
  for ($attempt=1; $attempt -le 3; $attempt++) {
   Select-FixturePath $picker $edit $fixture
   for ($i=0; $i -lt 40; $i++) { if ($null -eq (Find-Dialog "Choose the character's sBot executable")) { break }; Start-Sleep -Milliseconds 150 }
   if ($null -eq (Find-Dialog "Choose the character's sBot executable")) { break }
   Write-Output ("Retry ${attempt}: picker still open after Enter.")
  }
  Wait-DialogGone "Choose the character's sBot executable"
 Get-Window; $settings=Dialog 'Character settings'
 Check ((Find-Id $settings 'ExecutablePath').GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value -eq $fixture) 'Browse selection lands in the settings window'
 Invoke-Id $settings 'SaveAccount'; Wait-DialogGone 'Character settings'; Get-Window
 $ready=$false
  for ($i=0; $i -lt 30 -and !$ready; $i++) { Start-Sleep -Milliseconds 300; $ready=(Find-Id $script:window 'BotActionAccountA').Current.IsEnabled }
 Check $ready 'Saved executable becomes ready to open'
 Check (@(Get-Process -Name BotFixture -ErrorAction SilentlyContinue).Count -eq 0) 'Selecting executable did not launch it'

 for ($i=5; $i -le 10; $i++) {
  # Let the app fully settle its refresh cycle after each save before the next click.
  Start-Sleep -Milliseconds 1500
  try { Invoke-Id $script:window 'AddAccount' } catch {
   Write-Output ("--- diagnostics before add-click #" + $i + " ---")
   Write-Output ("Summary: " + (Find-Id $script:window 'Summary').Current.Name)
   $bulk=Find-Id $script:window 'StartTrainingAll'
   Write-Output ("BulkAll IsEnabled: " + $bulk.Current.IsEnabled)
   Get-Window
   Write-Output ("Fresh-window AddAccount: " + ($null -ne (Find-Id $script:window 'AddAccount')))
   Start-Sleep -Milliseconds 3000
   Write-Output ("AddAccount after 3s settle: " + ($null -ne (Find-Id $script:window 'AddAccount')))
   $all=$script:window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
   $ids=@(); foreach ($e in $all) { if ($e.Current.AutomationId) { $ids += $e.Current.AutomationId } }
   Write-Output ("Ids: " + (($ids | Sort-Object -Unique) -join ','))
   $panel=Find-Id $script:window 'ActivityPanel'
   $texts=@(); foreach ($e in $panel.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)) { if ($e.Current.ControlType -eq [System.Windows.Automation.ControlType]::Text) { $texts += $e.Current.Name } }
   Write-Output ("Log: " + (($texts | Select-Object -Last 12) -join ' | '))
   throw
  }
  $dialog=Dialog 'Character settings'
  Set-Value (Find-Id $dialog 'CharacterName') "Character$i"
  Invoke-Id $dialog 'SaveAccount'; Wait-DialogGone 'Character settings'; Get-Window
 }
 $add=Find-Id $script:window 'AddAccount'
 Check ($null -eq $add -or $add.Current.IsOffscreen) 'Add card disappears at ten accounts'
 Check ((Find-Id $script:window 'Summary').Current.Name -like '10/10*') 'Summary reports ten-account limit'
 Invoke-Id $script:window 'ConfigureCharacter10'; $dialog=Dialog 'Character settings'
 Invoke-Id $dialog 'RemoveAccount'; $confirmation=Dialog 'Remove account'
 Dialog-Click $confirmation 6; Wait-DialogGone 'Remove account'; Wait-DialogGone 'Character settings'; Get-Window
 Check ($null -ne (Find-Id $script:window 'AddAccount')) 'Add card returns after removal'
 $saved=[IO.File]::ReadAllText("$data\settings.json") | ConvertFrom-Json
 Check ($saved.Accounts.Count -eq 9 -and $saved.Accounts[0].ExecutablePath -eq $fixture) 'Account list and executable persisted on disk'

  Invoke-Id $script:window 'TerminateAll'; Start-Sleep -Milliseconds 300
   Check ((Find-Id $script:window 'BotActionAccountA').Current.IsEnabled) 'Terminate All runs without confirmation and leaves stopped workspace ready to launch'

 $closePattern=$script:window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
 $closePattern.Close(); Start-Sleep -Milliseconds 500
 $process.Refresh()
 Check (!$process.HasExited -and $process.MainWindowHandle -eq 0) 'Close-to-tray keeps manager alive with window hidden'
 # The isolated test instance is deliberately ended; no bot processes were started.
 Stop-Process -Id $process.Id
 $process.WaitForExit()
 $process=Start-Process -FilePath $AppPath -ArgumentList @('--data-dir',"`"$data`"") -PassThru
 for ($i=0; $i -lt 50; $i++) { Start-Sleep -Milliseconds 200; $process.Refresh(); if ($process.MainWindowHandle -ne 0) { break } }
 Get-Window
 Start-Sleep -Milliseconds 1000
    Check ((Find-Id $script:window 'Summary').Current.Name -like '9/10*' -and (Find-Id $script:window 'BotActionAccountA').Current.IsEnabled) 'Settings survive full application restart'
  $settingsBefore=[IO.File]::ReadAllText("$data\settings.json")
   Invoke-Id $script:window 'OpenSettings'; $notifDialog=Dialog 'Settings - i1988 - sBot manager'
     Check ($null -ne (Find-Id $notifDialog 'CloseToTrayPreference') -and $null -ne (Find-Id $notifDialog 'AutoHideBotsPreference') -and $null -ne (Find-Id $notifDialog 'AutoHideClientsPreference') -and $null -ne (Find-Id $notifDialog 'AutoClientlessAfterGamePreference') -and $null -ne (Find-Id $notifDialog 'StartWithWindowsPreference') -and $null -ne (Find-Id $notifDialog 'LaunchBotsAtWindowsStartupPreference') -and $null -ne (Find-Id $notifDialog 'StartupLaunchModePreference') -and $null -ne (Find-Id $notifDialog 'TerminateSessionBotsOnExitPreference') -and $null -ne (Find-Id $notifDialog 'NotificationsMasterPreference') -and $null -ne (Find-Id $notifDialog 'BotStoppedNotificationPreference') -and (Find-Id $notifDialog 'CloseToTrayPreference').Current.ControlType -eq [System.Windows.Automation.ControlType]::Button -and (Find-Id $notifDialog 'CloseToTrayPreference').Current.BoundingRectangle.Left -lt (Find-Id $notifDialog 'TerminateSessionBotsOnExitPreference').Current.BoundingRectangle.Left -and !(Find-Id $notifDialog 'LaunchBotsAtWindowsStartupPreference').Current.IsEnabled -and !(Find-Id $notifDialog 'StartupLaunchModePreference').Current.IsEnabled) 'Settings contains clientless, grouped two-column, and guarded startup controls'
   $scrollBars=$notifDialog.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::ScrollBar))
   Check ($scrollBars.Count -eq 0 -and (Find-Id $notifDialog 'SaveSettings').Current.BoundingRectangle.Bottom -le $notifDialog.Current.BoundingRectangle.Bottom) 'Settings fits its content without a scrollbar'
   Invoke-Id $notifDialog 'CancelSettings'; Wait-DialogGone 'Settings - i1988 - sBot manager'
  Check (([IO.File]::ReadAllText("$data\settings.json")) -eq $settingsBefore) 'Cancelling settings changes no settings file'
   Invoke-Id $script:window 'OpenSettings'; $notifDialog=Dialog 'Settings - i1988 - sBot manager'
   Toggle-Id $notifDialog 'AutoHideBotsPreference'; Toggle-Id $notifDialog 'AutoHideClientsPreference'; Invoke-Id $notifDialog 'SaveSettings'; Wait-DialogGone 'Settings - i1988 - sBot manager'
  $saved=[IO.File]::ReadAllText("$data\settings.json") | ConvertFrom-Json
  Check ($saved.AutoHideBots -eq $false -and $saved.AutoHideClients -eq $false) 'Settings saves auto-hide preferences together'
  Invoke-Id $script:window 'ExitManager'
  Check ($process.WaitForExit(5000)) 'Explicit Exit closes the manager without a confirmation'
 Write-Output "$script:checks UI checks passed. Isolated data: $data"
} finally {
 if (!$process.HasExited) { Stop-Process -Id $process.Id }
}
