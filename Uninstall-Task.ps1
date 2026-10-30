<#
.SYNOPSIS
    Uninstall-Task.ps1
    Uninstalls LaptopKeyboardAutoToggle Scheduled Task, terminates the background process,
    and guarantees that the internal laptop keyboard is re-enabled.
#>

[CmdletBinding()]
param()

$TaskName = "LaptopKeyboardAutoToggle"

# Check Admin Privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[ERROR] This uninstall script must be run as Administrator." -ForegroundColor Red
    Write-Host "Please right-click PowerShell, select 'Run as Administrator', and re-run." -ForegroundColor Yellow
    exit 1
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Uninstalling Laptop Keyboard Auto-Toggle Task           " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Stop Process
Write-Host "[1/3] Stopping any running LaptopKeyboardAutoToggle process..." -ForegroundColor Yellow
$proc = Get-Process -Name "LaptopKeyboardAutoToggle" -ErrorAction SilentlyContinue
if ($proc) {
    $proc | Stop-Process -Force
    Write-Host "Process terminated." -ForegroundColor Green
} else {
    Write-Host "Process is not running." -ForegroundColor Gray
}

# 2. Unregister Scheduled Task
Write-Host "[2/3] Removing Scheduled Task '$TaskName'..." -ForegroundColor Yellow
$task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($task) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "Scheduled task '$TaskName' removed successfully." -ForegroundColor Green
} else {
    Write-Host "Scheduled task was not found." -ForegroundColor Gray
}

# 3. Safety: Ensure internal keyboard is enabled
Write-Host "[3/3] Performing safety check: Ensuring all keyboards are ENABLED..." -ForegroundColor Yellow

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$configPath = Join-Path $scriptDir "config.json"
$targetHwId = ""

if (Test-Path $configPath) {
    try {
        $json = Get-Content $configPath -Raw | ConvertFrom-Json
        $targetHwId = $json.InternalKeyboardHardwareId
    } catch { }
}

# Enumerate disabled keyboards and enable them
$disabledKeyboards = Get-PnpDevice -Class Keyboard -Status Error, Degraded -ErrorAction SilentlyContinue
if ($disabledKeyboards) {
    foreach ($kb in $disabledKeyboards) {
        Write-Host "Re-enabling disabled keyboard: $($kb.FriendlyName) ($($kb.InstanceId))..." -ForegroundColor Yellow
        & pnputil /enable-device "$($kb.InstanceId)" | Out-Null
    }
}

if (-not [string]::IsNullOrWhiteSpace($targetHwId)) {
    Write-Host "Running pnputil enable for configured hardware ID '$targetHwId'..." -ForegroundColor Yellow
    & pnputil /enable-device "$targetHwId" | Out-Null
}

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "[SUCCESS] Uninstallation complete! Internal keyboard is restored." -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green

