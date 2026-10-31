# LaptopKeyboardAutoToggle v1.1.0

A lightweight utility that automatically disables a laptop's internal keyboard when a wireless keyboard USB dongle is connected, and restores it when unplugged. Features a live system tray indicator and a manual override dashboard.

## 🆕 What's new in 1.1.0 (Ubuntu / Debian)

The Windows app is unchanged. The Linux package was fixed so it works out of the box:

* **Choose Wireless Dongle** menu in the top-bar icon: pick your receiver from the connected USB devices instead of editing vendor/product IDs by hand. Previously the app only knew one hard-coded dongle and silently did nothing with any other.
* The internal keyboard is now always restored when the app is stopped or you log out, even during the first few seconds while the kernel is still unbinding it.
* Only one instance runs at a time (autostart + app-menu launch no longer fight).
* Resuming from **Pause** re-applies the dongle state immediately.
* The tray status reflects what the kernel actually did, not what was requested.
* The root helper rejects anything other than a `serioN` device name.
* Uninstalling stops the running app and re-enables the internal keyboard.
* CI now installs and removes the `.deb` on Ubuntu 22.04 and 24.04 before every release.

---

## 📦 Downloads

* **Windows (x64 Single-File):** `LaptopKeyboardAutoToggle-win-x64.zip`
* **Ubuntu / Debian (.deb Package):** `laptop-keyboard-auto-toggle_1.1-1_all.deb`

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
Download `laptop-keyboard-auto-toggle_1.1-1_all.deb` and install it in your terminal (upgrades 1.0 in place):
```bash
sudo apt install ./laptop-keyboard-auto-toggle_1.1-1_all.deb
```
Then start **Laptop Keyboard Auto-Toggle** from the app menu once (or log out and back in).

### 2. Pick your dongle
Click the keyboard icon in the top bar → **Choose Wireless Dongle** → select your receiver. The choice is saved to `~/.config/laptop-keyboard-auto-toggle/config.json`.

### 3. How It Works
* The app automatically starts on desktop login (`/etc/xdg/autostart/`).
* Uses Linux kernel `serio0` bus unbinding to silence the internal keyboard natively with zero third-party drivers needed.
* Shows an AppIndicator icon in your Ubuntu top bar for instant manual ON/OFF switching. On Debian GNOME, install `gnome-shell-extension-appindicator` for the icon.
