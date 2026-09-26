# GreatestSRO — sBot Farm Manager baseline

Original batch script supplied by Ibrahim, preserved without fixes for brainstorming.
This is a reference document, not a runnable batch file. The script has not been executed or runtime-tested.

## Confirmed context

- Four accounts: Xar, Xer, Xor, Xur.
- Each account has its own sBot folder.
- Current phase: brainstorming; no implementation changes approved.

## Original script

```bat
@echo off
setlocal EnableExtensions EnableDelayedExpansion

:: ================================================================
::              GREATESTSRO - SBOT FARM MANAGER
:: ================================================================

:: ------------------------- SETTINGS ------------------------------
set "ROOT=E:\Games\Silkroad\GreatestSRO\bot\sBot"
set "SBOT=SBotP_1.0.51.exe"

set "BOT1=Xar"
set "BOT2=Xer"
set "BOT3=Xor"
set "BOT4=Xur"

:: Seconds between bot launches
set "START_DELAY=3"

:: Seconds between watchdog checks
set "WATCHDOG_DELAY=10"

:: Wait before minimizing after Start All
set "AUTO_MINIMIZE_DELAY=5"

:: 1 = automatically minimize SBot windows after Start All
:: 0 = leave windows alone
set "AUTO_MINIMIZE=1"

set "LOGDIR=%ROOT%\FarmLogs"

:: ----------------------------------------------------------------

if not exist "%LOGDIR%" mkdir "%LOGDIR%" >nul 2>&1

for /f %%A in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd"') do set "TODAY=%%A"
set "LOGFILE=%LOGDIR%\Farm_%TODAY%.log"

title GreatestSRO - SBot Farm Manager
mode con cols=78 lines=38
color 0A

call :LOG "================================================"
call :LOG "Farm Manager started"
call :LOG "================================================"

goto MENU


:: ================================================================
::                           MAIN MENU
:: ================================================================

:MENU
cls
call :HEADER
call :STATUS

echo.
echo  ==========================================================================
echo.
echo       [1]  START ALL 4
echo       [2]  START Xar
echo       [3]  START Xer
echo       [4]  START Xor
echo       [5]  START Xur
echo.
echo       [6]  LIVE WATCHDOG
echo       [7]  RESTART ALL
echo       [8]  STOP ALL SBOTS
echo.
echo       [M]  MINIMIZE FARM WINDOWS
echo       [H]  HIDE FARM WINDOWS
echo       [S]  SHOW FARM WINDOWS
echo.
echo       [L]  OPEN LOG FOLDER
echo       [F]  OPEN SBOT ROOT FOLDER
echo       [R]  REFRESH DASHBOARD
echo       [0]  EXIT MANAGER
echo.
echo  ==========================================================================
echo.

choice /C 12345678MHSLFR0 /N /M "  Command: "

if errorlevel 15 goto EXIT
if errorlevel 14 goto MENU
if errorlevel 13 goto OPENROOT
if errorlevel 12 goto OPENLOGS
if errorlevel 11 goto SHOWFARM
if errorlevel 10 goto HIDEFARM
if errorlevel 9 goto MINIMIZEFARM
if errorlevel 8 goto STOPALL
if errorlevel 7 goto RESTARTALL
if errorlevel 6 goto WATCHDOG
if errorlevel 5 goto STARTXUR
if errorlevel 4 goto STARTXOR
if errorlevel 3 goto STARTXER
if errorlevel 2 goto STARTXAR
if errorlevel 1 goto STARTALL

goto MENU


:: ================================================================
::                      INDIVIDUAL STARTS
:: ================================================================

:STARTXAR
call :STARTBOT "%BOT1%"
timeout /t 1 /nobreak >nul
goto MENU

:STARTXER
call :STARTBOT "%BOT2%"
timeout /t 1 /nobreak >nul
goto MENU

:STARTXOR
call :STARTBOT "%BOT3%"
timeout /t 1 /nobreak >nul
goto MENU

:STARTXUR
call :STARTBOT "%BOT4%"
timeout /t 1 /nobreak >nul
goto MENU


:: ================================================================
::                         START ALL
:: ================================================================

:STARTALL
cls
call :HEADER

echo.
echo       ==============================================
echo                FARM STARTUP SEQUENCE
echo       ==============================================
echo.

call :STARTBOT "%BOT1%"
call :COUNTDOWN "%START_DELAY%"

call :STARTBOT "%BOT2%"
call :COUNTDOWN "%START_DELAY%"

call :STARTBOT "%BOT3%"
call :COUNTDOWN "%START_DELAY%"

call :STARTBOT "%BOT4%"

echo.
echo       ----------------------------------------------
echo              ALL START COMMANDS COMPLETE
echo       ----------------------------------------------

call :LOG "Start All sequence completed"

if "%AUTO_MINIMIZE%"=="1" (
    echo.
    echo       Auto-minimize in %AUTO_MINIMIZE_DELAY% seconds...
    timeout /t %AUTO_MINIMIZE_DELAY% /nobreak >nul
    call :MINIMIZEWINDOWS
)

echo.
echo       Returning to dashboard...
timeout /t 2 /nobreak >nul
goto MENU


:: ================================================================
::                         START BOT
:: ================================================================

:STARTBOT
set "BOT=%~1"
set "BOTDIR=%ROOT%\%BOT%"
set "BOTPATH=%BOTDIR%\%SBOT%"

echo       Checking %-4BOT%...

if not exist "%BOTPATH%" (
    echo       [ERROR] %BOT% - %SBOT% NOT FOUND
    call :LOG "%BOT% - ERROR - executable not found"
    exit /b
)

call :ISRUNNING "%BOT%"

if "!RUNNING!"=="1" (
    echo       [SKIP ] %BOT% - already running
    call :LOG "%BOT% - skipped - already running"
    exit /b
)

echo       [START] %BOT% - launching...
call :LOG "%BOT% - launch requested"

start "" /MIN /D "%BOTDIR%" "%SBOT%"

timeout /t 2 /nobreak >nul

call :ISRUNNING "%BOT%"

if "!RUNNING!"=="1" (
    echo       [ OK  ] %BOT% - ONLINE
    call :LOG "%BOT% - started successfully"
) else (
    echo       [WARN ] %BOT% - process not detected yet
    call :LOG "%BOT% - WARNING - process not detected after launch"
)

exit /b


:: ================================================================
::                  DETECT SPECIFIC SBOT COPY
:: ================================================================

:ISRUNNING
set "CHECKBOT=%~1"
set "RUNNING=0"

for /f %%A in ('powershell -NoProfile -Command ^
"$target='%ROOT%\%CHECKBOT%\%SBOT%'; $p=Get-CimInstance Win32_Process -Filter \"Name='%SBOT%'\" -ErrorAction SilentlyContinue ^| Where-Object {$_.ExecutablePath -ieq $target}; if($p){'1'}else{'0'}"') do (
    set "RUNNING=%%A"
)

exit /b


:: ================================================================
::                           STATUS
:: ================================================================

:STATUS
set /a ONLINE=0

echo.
echo       +----------------------------------------------------------+
echo       ^|                    FARM STATUS                           ^|
echo       +----------------------------------------------------------+

for %%B in (%BOT1% %BOT2% %BOT3% %BOT4%) do (
    call :ISRUNNING "%%B"

    if "!RUNNING!"=="1" (
        echo       ^|  %%B                 [ RUNNING ]                       ^|
        set /a ONLINE+=1
    ) else (
        echo       ^|  %%B                 [ OFFLINE ]                       ^|
    )
)

echo       +----------------------------------------------------------+
echo       ^|  Active SBot Processes: !ONLINE! / 4                          ^|
echo       +----------------------------------------------------------+

call :SYSTEMINFO

exit /b


:: ================================================================
::                        SYSTEM INFO
:: ================================================================

:SYSTEMINFO
set "CPU=?"
set "RAMUSED=?"
set "RAMTOTAL=?"
set "UPTIME=?"

for /f "tokens=*" %%A in ('powershell -NoProfile -Command ^
"$x=(Get-CimInstance Win32_Processor -ErrorAction SilentlyContinue ^| Measure-Object LoadPercentage -Average).Average; if($null -eq $x){0}else{[math]::Round($x)}"') do set "CPU=%%A"

for /f "tokens=*" %%A in ('powershell -NoProfile -Command ^
"$o=Get-CimInstance Win32_OperatingSystem; [math]::Round(($o.TotalVisibleMemorySize-$o.FreePhysicalMemory)/1MB,1)"') do set "RAMUSED=%%A"

for /f "tokens=*" %%A in ('powershell -NoProfile -Command ^
"$o=Get-CimInstance Win32_OperatingSystem; [math]::Round($o.TotalVisibleMemorySize/1MB,1)"') do set "RAMTOTAL=%%A"

for /f "tokens=*" %%A in ('powershell -NoProfile -Command ^
"$o=Get-CimInstance Win32_OperatingSystem; $u=(Get-Date)-$o.LastBootUpTime; '{0}d {1:00}h {2:00}m' -f $u.Days,$u.Hours,$u.Minutes"') do set "UPTIME=%%A"

echo.
echo       SYSTEM
echo       ------------------------------------------------------------
echo       CPU Load : %CPU%%%
echo       RAM Used : %RAMUSED% GB / %RAMTOTAL% GB
echo       PC Uptime: %UPTIME%

exit /b


:: ================================================================
::                         WATCHDOG
:: ================================================================

:WATCHDOG
call :LOG "Watchdog started"

:WATCHLOOP
cls
call :HEADER

echo.
echo       ==========================================================
echo                       LIVE FARM WATCHDOG
echo       ==========================================================
echo.
echo       Auto Recovery : ENABLED
echo       Check Interval: %WATCHDOG_DELAY% seconds
echo.
echo       CTRL+C = leave/terminate watchdog
echo.
echo       ----------------------------------------------------------

set /a WATCHONLINE=0

for %%B in (%BOT1% %BOT2% %BOT3% %BOT4%) do (

    call :ISRUNNING "%%B"

    if "!RUNNING!"=="1" (
        echo       [ OK ] %%B          RUNNING
        set /a WATCHONLINE+=1
    ) else (
        echo       [ !! ] %%B          OFFLINE
        echo              ^>^> AUTO-RESTARTING %%B...
        call :LOG "WATCHDOG - %%B detected offline"
        call :STARTBOT "%%B"
    )
)

echo.
echo       ----------------------------------------------------------
echo       SBot Health: !WATCHONLINE! / 4 detected
echo       ----------------------------------------------------------

call :SYSTEMINFO

echo.
echo       ----------------------------------------------------------
echo       Next health check in %WATCHDOG_DELAY% seconds...
echo       ----------------------------------------------------------

timeout /t %WATCHDOG_DELAY% /nobreak >nul
goto WATCHLOOP


:: ================================================================
::                      MINIMIZE FARM
:: ================================================================

:MINIMIZEFARM
cls
call :HEADER

echo.
echo       Minimizing SBot / Silkroad windows...
echo.

call :MINIMIZEWINDOWS

call :LOG "Farm windows minimize command executed"

echo       Done.
timeout /t 2 /nobreak >nul
goto MENU


:MINIMIZEWINDOWS
powershell -NoProfile -Command ^
"$sig='[DllImport(\"user32.dll\")] public static extern bool ShowWindowAsync(IntPtr hWnd,int nCmdShow);'; Add-Type -MemberDefinition $sig -Name Win32Show -Namespace Native -ErrorAction SilentlyContinue; Get-Process -ErrorAction SilentlyContinue ^| Where-Object {$_.MainWindowHandle -ne 0 -and ($_.ProcessName -like '*sbot*' -or $_.ProcessName -like '*sro*' -or $_.ProcessName -like '*silkroad*')} ^| ForEach-Object {[Native.Win32Show]::ShowWindowAsync($_.MainWindowHandle,6) ^| Out-Null}" >nul 2>&1

exit /b


:: ================================================================
::                         HIDE FARM
:: ================================================================

:HIDEFARM
cls
call :HEADER

echo.
echo       Hiding SBot / Silkroad windows...
echo       Processes will continue running.
echo.

call :HIDEWINDOWS

call :LOG "Farm windows hidden"

echo       Farm windows are now hidden.
echo.
echo       Use [S] SHOW FARM to restore them.
timeout /t 3 /nobreak >nul
goto MENU


:HIDEWINDOWS
powershell -NoProfile -Command ^
"$sig='[DllImport(\"user32.dll\")] public static extern bool ShowWindowAsync(IntPtr hWnd,int nCmdShow);'; Add-Type -MemberDefinition $sig -Name Win32Hide -Namespace Native -ErrorAction SilentlyContinue; Get-Process -ErrorAction SilentlyContinue ^| Where-Object {$_.MainWindowHandle -ne 0 -and ($_.ProcessName -like '*sbot*' -or $_.ProcessName -like '*sro*' -or $_.ProcessName -like '*silkroad*')} ^| ForEach-Object {[Native.Win32Hide]::ShowWindowAsync($_.MainWindowHandle,0) ^| Out-Null}" >nul 2>&1

exit /b


:: ================================================================
::                         SHOW FARM
:: ================================================================

:SHOWFARM
cls
call :HEADER

echo.
echo       Restoring SBot / Silkroad windows...
echo.

call :SHOWWINDOWS

call :LOG "Farm windows restore command executed"

echo       Restore command completed.
timeout /t 3 /nobreak >nul
goto MENU


:SHOWWINDOWS
powershell -NoProfile -Command ^
"$sig='[DllImport(\"user32.dll\")] public static extern bool ShowWindowAsync(IntPtr hWnd,int nCmdShow);'; Add-Type -MemberDefinition $sig -Name Win32Show2 -Namespace Native -ErrorAction SilentlyContinue; Get-Process -ErrorAction SilentlyContinue ^| Where-Object {$_.MainWindowHandle -ne 0 -and ($_.ProcessName -like '*sbot*' -or $_.ProcessName -like '*sro*' -or $_.ProcessName -like '*silkroad*')} ^| ForEach-Object {[Native.Win32Show2]::ShowWindowAsync($_.MainWindowHandle,9) ^| Out-Null}" >nul 2>&1

exit /b


:: ================================================================
::                         RESTART ALL
:: ================================================================

:RESTARTALL
cls
call :HEADER

echo.
echo       ==============================================
echo                   RESTARTING FARM
echo       ==============================================
echo.

choice /C YN /N /M "       Restart all SBot instances? [Y/N]: "

if errorlevel 2 goto MENU

call :LOG "Restart All requested"

echo.
echo       Closing SBot processes...

call :KILLBOT "%BOT1%"
call :KILLBOT "%BOT2%"
call :KILLBOT "%BOT3%"
call :KILLBOT "%BOT4%"

echo.
echo       Waiting 5 seconds...
timeout /t 5 /nobreak >nul

goto STARTALL


:: ================================================================
::                           STOP ALL
:: ================================================================

:STOPALL
cls
call :HEADER

echo.
echo       ==========================================================
echo                         STOP FARM
echo       ==========================================================
echo.
echo       This closes all four detected SBot processes.
echo.
echo       NOTE:
echo       This does NOT blindly kill every process on your PC.
echo       It targets the SBot executable inside the four farm folders.
echo.

choice /C YN /N /M "       Continue? [Y/N]: "

if errorlevel 2 goto MENU

call :LOG "Stop All requested"

echo.

call :KILLBOT "%BOT1%"
call :KILLBOT "%BOT2%"
call :KILLBOT "%BOT3%"
call :KILLBOT "%BOT4%"

echo.
echo       Stop sequence complete.
echo.

pause
goto MENU


:: ================================================================
::                          KILL BOT
:: ================================================================

:KILLBOT
set "KILLBOTNAME=%~1"

call :ISRUNNING "%KILLBOTNAME%"

if "!RUNNING!"=="0" (
    echo       [SKIP] %KILLBOTNAME% - already offline
    exit /b
)

echo       [STOP] %KILLBOTNAME%

powershell -NoProfile -Command ^
"$target='%ROOT%\%KILLBOTNAME%\%SBOT%'; Get-CimInstance Win32_Process -Filter \"Name='%SBOT%'\" -ErrorAction SilentlyContinue ^| Where-Object {$_.ExecutablePath -ieq $target} ^| ForEach-Object {Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue}" >nul 2>&1

call :LOG "%KILLBOTNAME% - stopped"

exit /b


:: ================================================================
::                           COUNTDOWN
:: ================================================================

:COUNTDOWN
set /a TIMER=%~1

:COUNTDOWNLOOP
if !TIMER! LEQ 0 exit /b

echo       Next launch in !TIMER!...
timeout /t 1 /nobreak >nul

set /a TIMER-=1
goto COUNTDOWNLOOP


:: ================================================================
::                            LOGGING
:: ================================================================

:LOG
for /f "tokens=*" %%A in ('powershell -NoProfile -Command "Get-Date -Format 'yyyy-MM-dd HH:mm:ss'"') do set "TIMESTAMP=%%A"

echo [%TIMESTAMP%] %~1>>"%LOGFILE%"

exit /b


:: ================================================================
::                           OPEN LOGS
:: ================================================================

:OPENLOGS
if not exist "%LOGDIR%" mkdir "%LOGDIR%" >nul 2>&1
start "" explorer.exe "%LOGDIR%"
goto MENU


:: ================================================================
::                           OPEN ROOT
:: ================================================================

:OPENROOT
start "" explorer.exe "%ROOT%"
goto MENU


:: ================================================================
::                             HEADER
:: ================================================================

:HEADER
echo.
echo  ==========================================================================
echo.
echo                  G R E A T E S T S R O
echo.
echo                    S B O T   F A R M
echo.
echo                     M A N A G E R
echo.
echo  ==========================================================================
exit /b


:: ================================================================
::                              EXIT
:: ================================================================

:EXIT
call :LOG "Farm Manager closed"

cls
echo.
echo  ==========================================================================
echo.
echo                      FARM MANAGER CLOSED
echo.
echo       Your running SBot/game processes were NOT terminated.
echo.
echo  ==========================================================================
echo.

timeout /t 2 /nobreak >nul
exit 
```
