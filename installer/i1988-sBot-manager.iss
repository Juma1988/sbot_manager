; Build with Inno Setup 6 after running installer\build-release.ps1.
; Keep AppId stable across versions so upgrades replace the existing installation.
#ifndef MyAppVersion
  #define MyAppVersion "0.0.1"
#endif

#define MyAppName "i1988 - sBot manager"
#define MyAppPublisher "i1988"
#define MyAppExeName "SBotManager.exe"

[Setup]
AppId={{8B133420-22AA-4C9C-9D3B-AD1B80198B01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\i1988\sBot manager
DefaultGroupName={#MyAppName}
OutputDir=..\releases\build
OutputBaseFilename=i1988-sBot-manager-setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
