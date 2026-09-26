param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '0.0.2'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $root 'publish\win-x64'
$release = Join-Path $root ("releases\" + $Version)
$iscc = Get-Command 'iscc.exe' -ErrorAction SilentlyContinue
if ($null -eq $iscc) { throw 'Inno Setup 6 is required. Install it, then rerun this script.' }

dotnet publish (Join-Path $root 'src\SBotManager\SBotManager.csproj') -c Release -r win-x64 --self-contained true -p:Version=$Version -o $publish
& $iscc.Source ("/DMyAppVersion=" + $Version) (Join-Path $PSScriptRoot 'i1988-sBot-manager.iss')

$installer = Join-Path $root ("releases\build\i1988-sBot-manager-setup-" + $Version + '.exe')
New-Item -ItemType Directory -Force -Path $release | Out-Null
Copy-Item -Force $installer $release
Get-FileHash -Algorithm SHA256 (Join-Path $release (Split-Path -Leaf $installer)) |
    Format-List Algorithm, Hash, Path | Out-File (Join-Path $release 'SHA256.txt')
Copy-Item -Force (Join-Path $PSScriptRoot 'i1988-sBot-manager.iss') $release
Write-Host "Release and rollback package created: $release"
