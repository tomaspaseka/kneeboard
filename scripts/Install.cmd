@echo off
setlocal

:: Are we already elevated?
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting administrator privileges...
    powershell.exe -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

:: Elevated from here on. Run the installer.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
if %errorlevel% neq 0 (
    echo.
    echo Installation failed with exit code %errorlevel%.
    pause
)
