# LaptopKeyboardAutoToggle

A lightweight Windows system tray & UI app built in **C# with .NET 8** that automatically silences your laptop's faulty internal keyboard when your wireless keyboard's USB dongle is connected, and restores it when unplugged. It also provides a **manual control UI window** and tray menu to turn the internal keyboard on or off anytime regardless of dongle status.

---

## 🔍 Hardware Detection Results on This Laptop

- **USB Dongle Hardware ID:** `VID_1EA7&PID_0066` (Wireless 2.4G / Bluetooth USB Composite Device)
- **Internal Laptop Keyboard:** `ACPI\HPQ8002\4&e9137ae&0` (`Standard 101/102-Key or Microsoft Natural PS/2 Keyboard for HP Hotkey Support`)
- **PnP Status:** Windows flags standard internal PS/2 keyboards (`ACPI\...`) with `CM_DEVCAP_NONDISABLEABLE`. Running `pnputil /disable-device` returns `Cannot disable critical system device`.

### How This App Solves the Critical System Device Restriction
Because Windows blocks standard PnP disable on internal PS/2 keyboards, this app implements a **Dual-Engine Architecture**:
1. **Interception Kernel Filter Driver (Primary for PS/2 keyboards):** Drops keystrokes from `ACPI\HPQ8002` at the kernel filter level in memory with 0ms latency. No reboots required to toggle on/off.
2. **SetupAPI / pnputil (For I2C/HID keyboards):** Toggles device power state via Windows Device Manager API.

---

## 🚀 Features

- **Automatic USB Event Detection:** Listens to `WM_DEVICECHANGE` (`DBT_DEVICEARRIVAL` and `DBT_DEVICEREMOVECOMPLETE`) for `VID_1EA7&PID_0066`. Zero polling, 0% idle CPU usage.
- **Manual Control UI Window:** Clickable dashboard to toggle the internal keyboard ON/OFF at will with just your mouse or touchpad.
- **System Tray Icon & Menu:**
  - Live status indicator (Internal KB ON/OFF, Dongle Plugged/Unplugged)
  - "Turn Internal Keyboard OFF / ON" manual switch
  - "Pause USB Automation" checkbox
  - "Enable internal keyboard now" (one-click safety restore)
  - "Show Dashboard..."
  - "Open Log File"
  - "Exit"
- **Power Management (Sleep & Hibernate Resume):** Listens to `WM_POWERBROADCAST`. If your laptop wakes from sleep without the dongle, the internal keyboard is automatically re-enabled.
- **Fail-Safe Crash & Exit Protection:** `AppDomain.UnhandledException`, `Application.ThreadException`, and `ProcessExit` automatically restore the keyboard if the app crashes or exits.
- **Emergency Global Hotkey:** Press `Ctrl + Alt + Shift + F12` to immediately re-enable the internal keyboard.
- **Task Scheduler Auto-Start:** Starts at Windows logon with elevated privileges (no UAC prompt).

---

## 🛠️ Build & Installation

### Step 1: Test Disabling on Your Machine
Run the included diagnostic script in an elevated PowerShell prompt:
```powershell
.\Test-DisableKeyboard.ps1
```
*If your keyboard returns `Cannot disable critical system device` (like this HP PS/2 keyboard), proceed to Step 2 to install the Interception driver once.*

### Step 2: One-Time Interception Driver Setup (For PS/2 Keyboards)
Run in elevated PowerShell:
```powershell
.\Setup-Interception.ps1
```
> **Note:** A one-time computer restart is required after running this so the Windows kernel loads the filter driver.

### Step 3: Build the App (.NET 8)
```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false
```
*The output executable will be placed in `bin\Release\net8.0-windows\win-x64\publish\`.*

### Step 4: Install as Auto-Starting Task (No UAC)
In elevated PowerShell:
```powershell
.\Install-Task.ps1
```
To uninstall at any time:
```powershell
.\Uninstall-Task.ps1
```

---

## 🧪 Manual Test Checklist

| # | Test Scenario | Expected Outcome |
|---|---|---|
| 1 | **Normal Plug In** | Plug in USB dongle (`VID_1EA7&PID_0066`). Tray icon updates, internal keyboard is silenced, external wireless keyboard works. |
| 2 | **Normal Unplug** | Unplug USB dongle. Tray icon updates, internal keyboard immediately starts typing again. |
| 3 | **Manual UI Override** | Open dashboard from tray icon. Click "Turn Keyboard OFF" while dongle is unplugged. Internal keyboard stops responding. Click "Turn Keyboard ON" to restore. |
| 4 | **Sleep / Resume (Dongle Present)** | Put laptop to sleep with dongle plugged in. Resume laptop. Internal keyboard stays disabled. |
| 5 | **Sleep / Resume (Dongle Removed during sleep)** | Put laptop to sleep with dongle in. Unplug dongle while asleep. Wake laptop. Internal keyboard is automatically enabled. |
| 6 | **External Keyboard Dead Battery / Turned Off** | While dongle remains plugged in, right-click tray icon with touchpad/mouse and click **"Enable internal keyboard now"**. Internal keyboard starts working immediately. |
| 7 | **Emergency Hotkey** | When internal keyboard is disabled, press `Ctrl + Alt + Shift + F12`. Internal keyboard is restored instantly with a balloon tip. |
| 8 | **App Crash / Task Kill** | Open Task Manager or run `taskkill /f /im LaptopKeyboardAutoToggle.exe`. Exit handler fires and restores keyboard. |

