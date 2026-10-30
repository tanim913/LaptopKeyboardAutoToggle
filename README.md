# LaptopKeyboardAutoToggle

A lightweight, automated utility for **Windows** and **Linux (Ubuntu / Debian)** that automatically silences your laptop's faulty internal keyboard when your wireless keyboard's USB dongle is connected, and restores it when unplugged. 

It also provides an interactive **system tray indicator & manual control dashboard** to toggle the internal keyboard ON or OFF anytime, regardless of whether the dongle is plugged in.

---

## 📥 Downloads & Releases

Pre-compiled packages are available on the [**GitHub Releases**](https://github.com/tanim913/LaptopKeyboardAutoToggle/releases) page:
* 🪟 **Windows (x64 Single-File Bundle):** `LaptopKeyboardAutoToggle-win-x64.zip`
* 🐧 **Ubuntu / Debian (.deb Package):** `laptop-keyboard-auto-toggle_1.0-1_all.deb`

---

## 🪟 Windows Setup & Usage

### Why Special Handling is Needed on Windows
Standard Windows PnP flags internal PS/2 keyboards (`ACPI\...`) with `CM_DEVCAP_NONDISABLEABLE`. Running `pnputil /disable-device` returns `Cannot disable critical system device`. 

To solve this, this app uses a **Dual-Engine Architecture**:
1. **Interception Kernel Filter Driver (Primary for PS/2 keyboards):** Drops keystrokes from the internal keyboard at the driver filter level in memory with 0ms latency.
2. **SetupAPI / PnP (For I2C/HID keyboards):** Toggles device power state via Windows Device Manager API.

### Windows Installation Steps
1. Download and extract **`LaptopKeyboardAutoToggle-win-x64.zip`**.
2. **One-Time Driver Setup:** Open PowerShell as Administrator and run:
   ```powershell
   Set-ExecutionPolicy -Scope Process Bypass -Force; .\Setup-Interception.ps1
   ```
   *Restart your laptop once so the kernel loads the filter.*
3. **Enable Auto-Start on Boot (No UAC):**
   Double-click **`Install-AutoStart.bat`** (click "Yes" on the UAC prompt).

### Windows Controls & Shortcuts
* **Dashboard Window:** Click or double-click the tray icon to open the Dashboard and click **"Turn Keyboard OFF / ON"** with your mouse or touchpad.
* **Emergency Hotkey:** Press **`Ctrl + Alt + Shift + F12`** anytime to instantly restore your internal keyboard.
* **Configuration:** Edit `config.json` next to the executable to customize hardware IDs or hotkeys.

---

## 🐧 Ubuntu / Debian Linux Setup & Usage

### How It Works on Linux
On Linux, there is no "critical system device" block. The kernel natively supports unbinding the internal AT keyboard from the `serio` bus (`/sys/bus/serio/drivers/atkbd/unbind`). This silences the hardware at the kernel level across both **Wayland** and **X11** with zero third-party drivers.

### Linux Installation Steps
1. Download **`laptop-keyboard-auto-toggle_1.0-1_all.deb`**.
2. Install via Terminal:
   ```bash
   sudo dpkg -i laptop-keyboard-auto-toggle_1.0-1_all.deb
   sudo apt-get install -f
   ```
3. The app adds an auto-start desktop entry (`/etc/xdg/autostart/`) and launches automatically on login.

### Linux Controls & Configuration
* **Top Bar Tray Icon:** Shows live status (`Internal KB: OFF | Dongle: PLUGGED`) and lets you manually switch the keyboard ON/OFF.
* **Configuration:** Edit `~/.config/laptop-keyboard-auto-toggle/config.json` to change the target USB vendor/product ID:
  ```json
  {
    "usb_vendor_id": "1ea7",
    "usb_product_id": "0066",
    "serio_device": "serio0"
  }
  ```

---

## 📁 Repository Structure

```text
LaptopKeyboardAutoToggle/
├── .github/workflows/build.yml   # Automated GitHub Actions build & release
├── MainForm.cs                   # Windows Dashboard UI window
├── TrayApplicationContext.cs     # Windows system tray & Win32 event listener
├── DeviceManagerService.cs       # Windows state machine & hardware detection
├── InterceptionFilter.cs         # Windows Interception driver C# wrapper
├── Install-AutoStart.bat         # 1-click Windows Task Scheduler installer
├── Uninstall-AutoStart.bat       # 1-click Windows uninstaller
├── Setup-Interception.ps1        # Windows driver installer
├── linux/                        # Linux packaging source
│   ├── build_deb.py              # Script to build .deb package
│   └── laptop-keyboard-auto-toggle_1.0-1_all/
│       ├── DEBIAN/control        # Debian package metadata
│       ├── usr/bin/              # Python daemon & serio helper script
│       └── etc/                  # Sudoers permissions & autostart entry
└── LICENSE                       # MIT License
```

---

## 📄 License
This project is licensed under the [MIT License](LICENSE).
