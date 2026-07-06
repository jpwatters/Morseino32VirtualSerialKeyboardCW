@echo off
REM Double-clickable wrapper around install.ps1.
REM PowerShell scripts don't run on double-click by default (and a
REM machine's default execution policy often blocks them entirely), so this
REM launches install.ps1 with the bypass policy for just this one run --
REM it does not change any system-wide PowerShell settings.

setlocal
set SCRIPT_DIR=%~dp0

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%install.ps1" %*

echo.
echo Press any key to close this window...
pause >nul
