# LaptopKeyboardAutoToggle v1.2.0

A lightweight utility that automatically disables a laptop's internal keyboard when a wireless keyboard USB dongle is connected, and restores it when unplugged. Features a live system tray indicator and a manual override dashboard.

## 🆕 What's new in 1.2.0 (Ubuntu / Debian)

The Windows app is unchanged. The Linux package is now fully automatic:

* **Works from boot, before login, with no app running.** A udev rule switches the internal keyboard: boot with the dongle plugged in and the laptop keyboard is off from the login screen on; boot without it and the laptop keyboard works; plug/unplug any time and it follows.
* **Unplugging always restores the laptop keyboard.** (1.1.0 missed the unplug event and could leave you with no working keyboard; do not use 1.1.0.)
* The internal keyboard **stays** disabled: the serio port is switched to manual bind mode, because otherwise the kernel re-attaches the keyboard on the next keystroke (including ghost keystrokes from a faulty keyboard).
* **Choose Wireless Dongle** menu in the top-bar icon: pick your receiver from the connected USB devices. The choice is system-wide (`/etc/laptop-keyboard-auto-toggle.conf`) so it applies at the login screen too.
* The tray icon is now just status and control (pause, manual ON/OFF). Quitting it does not stop the automation.
* The root helper validates every argument; uninstalling re-enables the internal keyboard.
* CI installs, exercises and removes the `.deb` on Ubuntu 22.04 and 24.04 before every release.

---

## 📦 Downloads

* **Windows (x64 Single-File):** `LaptopKeyboardAutoToggle-win-x64.zip`
* **Ubuntu / Debian (.deb Package):** `laptop-keyboard-auto-toggle_1.2.0-1_all.deb`

---

## 🪟 Windows Instructions

### 1. First-Time Setup (PS/2 Hardware Driver)
Standard Windows PnP blocks disabling internal PS/2 keyboards directly. Run this **once** to install the Interception filter driver:
1. Extract `LaptopKeyboardAutoToggle-win-x64.zip`.
2. Open PowerShell as Administrator, navigate to the folder, and run:
   ```powershell
   Set-ExecutionPolicy -Scope Process Bypass -Force; .\Setup-Interception.ps1
   ```
3. **Restart your laptop once.**

### 2. Enable Auto-Start on Boot (No UAC Prompts)
* Double-click **`Install-AutoStart.bat`** and click "Yes" on the UAC prompt.
* From now on, whenever your laptop boots with your wireless dongle plugged in, the internal keyboard is silenced automatically before you touch a key.

### 3. Manual Control & Safety
* **Dashboard:** Click the system tray icon to open the Dashboard and manually toggle the keyboard ON/OFF at will with your touchpad/mouse.
* **Emergency Hotkey:** Press **`Ctrl + Alt + Shift + F12`** anytime to instantly restore your internal keyboard.

---

## 🐧 Ubuntu / Debian Linux Instructions

### 1. Installation
Download `laptop-keyboard-auto-toggle_1.2.0-1_all.deb` and install it in your terminal (upgrades older versions in place):
```bash
sudo apt install ./laptop-keyboard-auto-toggle_1.2.0-1_all.deb
```
Auto-toggling is active immediately, no logout or reboot needed.

### 2. Pick your dongle (only if it isn't `1ea7:0066`)
Click the keyboard icon in the top bar → **Choose Wireless Dongle** → select your receiver. Or in a terminal, with the ID from `lsusb`:
```bash
sudo laptop-keyboard-toggle-helper set-dongle 1ea7:0066
```

### 3. How It Works
* A udev rule (`/lib/udev/rules.d/90-laptop-keyboard-auto-toggle.rules`) runs on every USB plug/unplug and at boot, so it works before login.
* Uses Linux kernel `serio0` bus unbinding to silence the internal keyboard natively with zero third-party drivers needed.
* The AppIndicator icon (auto-starts at login) shows status and offers pause and manual ON/OFF. On Debian GNOME, install `gnome-shell-extension-appindicator` for the icon.
