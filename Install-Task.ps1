<#
.SYNOPSIS
    Install-Task.ps1
    Installs LaptopKeyboardAutoToggle as a Windows Scheduled Task running at user logon
    with highest privileges (elevated admin without UAC prompts).
#>

[CmdletBinding()]
param(
    [string]$ExePath
)

$TaskName = "LaptopKeyboardAutoToggle"

# Check Admin Privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[ERROR] This installation script must be run as Administrator." -ForegroundColor Red
    Write-Host "Please right-click PowerShell, select 'Run as Administrator', and re-run." -ForegroundColor Yellow
    exit 1
}

# Locate Executable if not provided
if ([string]::IsNullOrWhiteSpace($ExePath)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $candidatePaths = @(
        (Join-Path $scriptDir "LaptopKeyboardAutoToggle.exe"),
        (Join-Path $scriptDir "publish\LaptopKeyboardAutoToggle.exe"),
        (Join-Path $scriptDir "bin\Release\net8.0-windows\win-x64\publish\LaptopKeyboardAutoToggle.exe"),
        (Join-Path $scriptDir "bin\Release\net8.0-windows\win-x64\LaptopKeyboardAutoToggle.exe")
    )

    foreach ($path in $candidatePaths) {
        if (Test-Path $path) {
            $ExePath = (Resolve-Path $path).Path
            break
        }
    }
}

if (-not (Test-Path $ExePath)) {
    Write-Host "[ERROR] Could not find LaptopKeyboardAutoToggle.exe." -ForegroundColor Red
    Write-Host "Please build the project first using: dotnet publish -c Release -r win-x64" -ForegroundColor Yellow
    Write-Host "Or specify the path: .\Install-Task.ps1 -ExePath 'C:\path\to\LaptopKeyboardAutoToggle.exe'" -ForegroundColor Yellow
    exit 1
}

$ExeDir = Split-Path -Parent $ExePath
Write-Host "[1/3] Target executable found at: $ExePath" -ForegroundColor Green

# 1. Action
$action = New-ScheduledTaskAction -Execute $ExePath -WorkingDirectory $ExeDir

# 2. Trigger: At Logon of current user
$trigger = New-ScheduledTaskTrigger -AtLogOn

# 3. Principal: Run with Highest Available privileges
$principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -RunLevel Highest -LogonType Interactive

# 4. Settings: Allow on battery, don't stop on battery switch
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries `
                                        -DontStopIfGoingOnBatteries `
                                        -ExecutionTimeLimit ([TimeSpan]::Zero) `
                                        -Priority 4 `
                                        -RestartCount 3 `
                                        -RestartInterval (New-TimeSpan -Minutes 1)

# 5. Register Task
Write-Host "[2/3] Registering Scheduled Task '$TaskName'..." -ForegroundColor Yellow
Register-ScheduledTask -TaskName $TaskName `
                       -Action $action `
                       -Trigger $trigger `
                       -Principal $principal `
                       -Settings $settings `
                       -Description "Runs Laptop Keyboard Auto-Toggle in system tray with admin rights at logon." `
                       -Force | Out-Null

# 6. Start Task Now
Write-Host "[3/3] Starting task now..." -ForegroundColor Yellow
Start-ScheduledTask -TaskName $TaskName

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "[SUCCESS] Task '$TaskName' installed and launched!" -ForegroundColor Green
Write-Host "The application will run automatically at logon with highest privileges." -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Green

