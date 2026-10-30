@echo off
setlocal EnableDelayedExpansion
title Laptop Keyboard Auto-Toggle Setup

echo ==========================================================
echo    Laptop Keyboard Auto-Toggle - One-Click Setup
echo ==========================================================
echo.

:: 1. Self-elevate to Administrator with UAC prompt
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [PROMPT] Requesting Administrator permission...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process cmd -ArgumentList '/c \"\"%~dpnx0\"\"' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"

echo [1/3] Checking Keyboard Filter Driver...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$filters = (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e96b-e325-11ce-bfc1-08002be10318}' -Name UpperFilters -ErrorAction SilentlyContinue).UpperFilters; if ($filters -contains 'keyboard') { exit 0 } else { exit 1 }"

if %errorlevel% equ 0 (
    echo [OK] Keyboard filter driver is already installed.
    set REBOOT_NEEDED=0
) else (
    echo [INFO] Installing Interception keyboard filter driver...
    if exist "Setup-Interception.ps1" (
        powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Setup-Interception.ps1"
    ) else if exist "..\Setup-Interception.ps1" (
        powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\Setup-Interception.ps1"
    )
    set REBOOT_NEEDED=1
)

echo.
echo [2/3] Installing Windows Auto-Start Task (Logon with Admin Rights)...
if exist "Install-Task.ps1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Task.ps1"
) else if exist "..\Install-Task.ps1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\Install-Task.ps1"
)

echo.
echo [3/3] Launching Laptop Keyboard Auto-Toggle...
if exist "LaptopKeyboardAutoToggle.exe" (
    start "" "LaptopKeyboardAutoToggle.exe"
) else if exist "publish\LaptopKeyboardAutoToggle.exe" (
    start "" "publish\LaptopKeyboardAutoToggle.exe"
)

echo.
echo ==========================================================
if "!REBOOT_NEEDED!"=="1" (
    echo [IMPORTANT] First-time setup complete!
    echo Please RESTART your computer once so Windows loads the driver.
    echo After reboot, your internal keyboard will turn off automatically!
) else (
    echo [SUCCESS] Setup is complete and the app is active!
    echo Your internal keyboard will turn off automatically when your
    echo wireless USB dongle is connected.
)
echo ==========================================================
echo.
pause
