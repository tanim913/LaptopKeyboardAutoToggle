<#
.SYNOPSIS
    Setup-Interception.ps1
    Helper script to download and install oblitum's Interception driver.
    This solves the "Cannot disable critical system device" issue on PS/2 laptop keyboards (e.g. ACPI\HPQ8002).
#>

[CmdletBinding()]
param(
    [switch]$Uninstall
)

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[ERROR] This script must be run as Administrator." -ForegroundColor Red
    Write-Host "Please right-click PowerShell, select 'Run as Administrator', and re-run." -ForegroundColor Yellow
    exit 1
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$zipPath = Join-Path $scriptDir "Interception.zip"
$extractDir = Join-Path $scriptDir "InterceptionPkg"

if ($Uninstall) {
    Write-Host "[ACTION] Uninstalling Interception driver..." -ForegroundColor Yellow
    $installer = Join-Path $extractDir "Interception\command line installer\install-interception.exe"
    if (Test-Path $installer) {
        & "$installer" /uninstall
        Write-Host "[DONE] Interception driver uninstalled. Please reboot your computer." -ForegroundColor Green
    } else {
        Write-Host "[ERROR] Installer not found at $installer" -ForegroundColor Red
    }
    exit 0
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Installing Interception Keyboard Filter Driver          " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Download if not present
if (-not (Test-Path $zipPath)) {
    Write-Host "[1/3] Downloading Interception driver package from GitHub..." -ForegroundColor Yellow
    $downloadUrl = "https://github.com/oblitum/Interception/releases/download/v1.0.1/Interception.zip"
    Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath -UseBasicParsing
    Write-Host "Downloaded successfully." -ForegroundColor Green
}

# 2. Extract
if (-not (Test-Path $extractDir)) {
    Write-Host "[2/3] Extracting driver package..." -ForegroundColor Yellow
    Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force
}

# 3. Copy interception.dll to app directory
$dllSource = Join-Path $extractDir "Interception\library\x64\interception.dll"
$dllDest = Join-Path $scriptDir "interception.dll"

if (Test-Path $dllSource) {
    Copy-Item -Path $dllSource -Destination $dllDest -Force
    Write-Host "Copied 64-bit interception.dll to application directory." -ForegroundColor Green
}

# 4. Run installer
$installer = Join-Path $extractDir "Interception\command line installer\install-interception.exe"
if (Test-Path $installer) {
    Write-Host "[3/3] Running driver installer: install-interception.exe /install..." -ForegroundColor Yellow
    & "$installer" /install
    Write-Host ""
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host "[SUCCESS] Interception driver has been registered!" -ForegroundColor Green
    Write-Host "NOTE: You MUST reboot your computer ONCE for the kernel driver to activate." -ForegroundColor Yellow
    Write-Host "After reboot, LaptopKeyboardAutoToggle will completely silence your internal keyboard" -ForegroundColor White
    Write-Host "in real time with 0 latency, while your wireless USB keyboard works perfectly!" -ForegroundColor White
    Write-Host "==========================================================" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Could not find install-interception.exe" -ForegroundColor Red
}

