@echo off
dotnet build "%~dp0src\SBotManager\SBotManager.csproj" --nologo
if errorlevel 1 (
    echo Build failed. Make sure the .NET 10 SDK is installed.
    pause
    exit /b 1
)
start "" "%~dp0src\SBotManager\bin\Debug\net10.0-windows\SBotManager.exe"
