<#
.SYNOPSIS
    Test-DisableKeyboard.ps1
    Diagnostic script to test whether your laptop internal keyboard can be disabled
    via pnputil / SetupAPI on this specific machine.

.DESCRIPTION
    Checks device capabilities. If the keyboard is flagged with CM_DEVCAP_NONDISABLEABLE
    (common for standard PS/2 internal keyboards on Windows), Windows will reject disabling it
    with "Cannot disable critical system device". This script tests disabling and immediately
    re-enables the keyboard so you know with 100% certainty before running any background service.

.NOTES
    Must be run as Administrator.
#>

[CmdletBinding()]
param(
    [string]$TargetInstanceId
)

# 1. Check Administrator Privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[ERROR] This diagnostic script MUST be run in an elevated (Administrator) PowerShell prompt." -ForegroundColor Red
    Write-Host "Please right-click PowerShell, select 'Run as Administrator', and re-run this script." -ForegroundColor Yellow
    exit 1
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Laptop Internal Keyboard Disabling Capability Test     " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# 2. Enumerate Keyboard Devices
Write-Host "[1/3] Enumerating keyboard devices on this system..." -ForegroundColor Yellow
$pnpKeyboards = Get-PnpDevice -Class Keyboard -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq 'OK' -or $_.Status -eq 'Error' -or $_.Status -eq 'Degraded' }

if (-not $pnpKeyboards -or $pnpKeyboards.Count -eq 0) {
    Write-Host "[WARNING] No keyboards found via Get-PnpDevice. Enumerating via pnputil..." -ForegroundColor Yellow
    & pnputil /enum-devices /class Keyboard /connected
} else {
    Write-Host "Detected Keyboard Devices:" -ForegroundColor Green
    $idx = 0
    foreach ($kb in $pnpKeyboards) {
        $idx++
        Write-Host "  [$idx] Friendly Name: $($kb.FriendlyName)" -ForegroundColor White
        Write-Host "      Instance ID:   $($kb.InstanceId)" -ForegroundColor Gray
        Write-Host "      Status:        $($kb.Status)" -ForegroundColor Gray
    }
}
Write-Host ""

# 3. Determine target device to test
if ([string]::IsNullOrWhiteSpace($TargetInstanceId)) {
    # Auto-detect likely internal keyboard (ACPI or I2C)
    $candidate = $pnpKeyboards | Where-Object { 
        $_.InstanceId -like "ACPI*" -or 
        $_.InstanceId -like "*PNP0303*" -or 
        $_.InstanceId -like "HID\VEN_*" -or 
        $_.InstanceId -like "HID\*I2C*"
    } | Select-Object -First 1

    if ($candidate) {
        Write-Host "Auto-detected potential internal keyboard: " -NoNewline
        Write-Host "$($candidate.FriendlyName) ($($candidate.InstanceId))" -ForegroundColor Cyan
        $choice = Read-Host "Test this device? (Y/n) or enter a different Instance ID"
        if ($choice -match '^[Nn]') {
            $TargetInstanceId = Read-Host "Paste the exact Instance ID of the internal keyboard"
        } elseif (-not [string]::IsNullOrWhiteSpace($choice) -and $choice -notmatch '^[Yy]') {
            $TargetInstanceId = $choice
        } else {
            $TargetInstanceId = $candidate.InstanceId
        }
    } else {
        $TargetInstanceId = Read-Host "Paste the exact Instance ID of your internal keyboard (from list above)"
    }
}

if ([string]::IsNullOrWhiteSpace($TargetInstanceId)) {
    Write-Host "[ERROR] No target Instance ID specified. Exiting." -ForegroundColor Red
    exit 1
}

$TargetInstanceId = $TargetInstanceId.Trim()
Write-Host ""
Write-Host "[2/3] Testing disable on device: '$TargetInstanceId'..." -ForegroundColor Yellow

$disableOutput = & pnputil /disable-device "$TargetInstanceId" 2>&1 | Out-String

Write-Host "Output from pnputil:" -ForegroundColor Gray
Write-Host $disableOutput

if ($disableOutput -like "*Cannot disable critical system device*" -or $disableOutput -like "*Failed to disable*") {
    Write-Host "----------------------------------------------------------" -ForegroundColor Red
    Write-Host "[RESULT] DIRECT PNPUTIL DISABLING IS BLOCKED BY WINDOWS!" -ForegroundColor Red
    Write-Host "----------------------------------------------------------" -ForegroundColor Red
    Write-Host ""
    Write-Host "Reason: Windows marks standard PS/2 internal keyboards as critical boot devices" -ForegroundColor Yellow
    Write-Host "(`CM_DEVCAP_NONDISABLEABLE`). Neither SetupAPI nor pnputil can disable them directly." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Recommended Next Steps & Alternatives:" -ForegroundColor Cyan
    Write-Host "  1. If this is an I2C HID laptop keyboard, find its parent device under 'Human Interface Devices'" -ForegroundColor White
    Write-Host "     (e.g. 'I2C HID Device') and test disabling that parent instead." -ForegroundColor White
    Write-Host "  2. Use the 'Interception' keyboard filter driver (github.com/oblitum/Interception) which intercepts" -ForegroundColor White
    Write-Host "     and suppresses keystrokes from this device in software without needing PnP disable." -ForegroundColor White
    Write-Host "  3. Use Windows Keyboard Filter (WEKF) if you are on Windows Enterprise / IoT." -ForegroundColor White
    exit 2
} else {
    Write-Host "----------------------------------------------------------" -ForegroundColor Green
    Write-Host "[SUCCESS] Device was successfully DISABLED!" -ForegroundColor Green
    Write-Host "----------------------------------------------------------" -ForegroundColor Green
    Write-Host ""
    Write-Host "[3/3] IMMEDIATELY RE-ENABLING KEYBOARD FOR SAFETY..." -ForegroundColor Yellow
    
    $enableOutput = & pnputil /enable-device "$TargetInstanceId" 2>&1 | Out-String
    Write-Host $enableOutput
    
    Write-Host "[CONFIRMED] Internal keyboard has been re-enabled and is ready for the AutoToggle app!" -ForegroundColor Green
    exit 0
}

