@echo off
setlocal
echo ==========================================================
echo  Installing Laptop Keyboard Auto-Toggle Task (Logon)
echo ==========================================================
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [PROMPT] Requesting Administrator privileges...
    powershell -NoProfile -Command "Start-Process cmd -ArgumentList '/c \"\"%~dpnx0\"\"' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Task.ps1"
echo.
pause

