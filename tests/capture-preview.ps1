param([Parameter(Mandatory=$true)][int]$PreviewProcessId)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class CapturePreview {
 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
}
'@
$p=Get-Process -Id $PreviewProcessId
[void][CapturePreview]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 400
$rect=New-Object CapturePreview+Rect
if (![CapturePreview]::GetWindowRect($p.MainWindowHandle,[ref]$rect)) { throw 'No preview window.' }
$bitmap=[Drawing.Bitmap]::new($rect.Right-$rect.Left,$rect.Bottom-$rect.Top)
$graphics=[Drawing.Graphics]::FromImage($bitmap); $dc=$graphics.GetHdc()
try { $ok=[CapturePreview]::PrintWindow($p.MainWindowHandle,$dc,2) } finally { $graphics.ReleaseHdc($dc) }
try { if (!$ok) { throw 'Capture failed.' }; $bitmap.Save([IO.Path]::GetFullPath("$PSScriptRoot\..\docs\qa\sample-preview.png"),[Drawing.Imaging.ImageFormat]::Png) }
finally { $graphics.Dispose(); $bitmap.Dispose() }
