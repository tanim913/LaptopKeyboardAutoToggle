# LaptopKeyboardAutoToggle v1.0.0

A lightweight utility that automatically disables a laptop's internal keyboard when a wireless keyboard USB dongle is connected, and restores it when unplugged. Features a live system tray indicator and a manual override dashboard.

---

## 📦 Downloads

* **Windows (x64 Single-File):** `LaptopKeyboardAutoToggle-win-x64.zip`
* **Ubuntu / Debian (.deb Package):** `laptop-keyboard-auto-toggle_1.0-1_all.deb`

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
Download `laptop-keyboard-auto-toggle_1.0-1_all.deb` and install it in your terminal:
```bash
sudo dpkg -i laptop-keyboard-auto-toggle_1.0-1_all.deb
sudo apt-get install -f
```

### 2. How It Works
* The daemon automatically starts on desktop login (`/etc/xdg/autostart/`).
* Uses Linux kernel `serio0` bus unbinding to silence the internal keyboard natively with zero third-party drivers needed.
* Shows an AppIndicator icon in your Ubuntu top bar for instant manual ON/OFF switching.

### 3. Configuration
Default settings listen for USB dongle `1ea7:0066`. To configure a different dongle:
Edit `~/.config/laptop-keyboard-auto-toggle/config.json`.
