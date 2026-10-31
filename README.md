# LaptopKeyboardAutoToggle

A lightweight, automated utility for **Windows** and **Linux (Ubuntu / Debian)** that automatically silences your laptop's faulty internal keyboard when your wireless keyboard's USB dongle is connected, and restores it when unplugged. 

It also provides an interactive **system tray indicator & manual control dashboard** to toggle the internal keyboard ON or OFF anytime, regardless of whether the dongle is plugged in.

🌐 **Website & Live Demo:** [https://tanim913.github.io/LaptopKeyboardAutoToggle/](https://tanim913.github.io/LaptopKeyboardAutoToggle/)

---

## 📥 Downloads & Releases

Pre-compiled packages are available on the [**GitHub Releases**](https://github.com/tanim913/LaptopKeyboardAutoToggle/releases) page:
* 🪟 **Windows (x64 Single-File Bundle):** `LaptopKeyboardAutoToggle-win-x64.zip`
* 🐧 **Ubuntu / Debian (.deb Package):** `laptop-keyboard-auto-toggle_1.1-1_all.deb`

---

## 🛠️ Technology Stack & Engineering Architecture

LaptopKeyboardAutoToggle was engineered from the ground up to operate at the hardware and driver boundary without polling loops or high CPU usage.

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                       Hardware Event (USB Dongle Plug/Unplug)               │
└───────────────────────┬─────────────────────────────────────────────────────┘
                        │
         ┌──────────────┴──────────────┐
         ▼                             ▼
┌─────────────────────────────┐ ┌─────────────────────────────┐
│       Windows Engine        │ │     Ubuntu/Linux Engine     │
├─────────────────────────────┤ ├─────────────────────────────┤
│ • C# 12 / .NET 8 LTS        │ │ • Python 3 & pyudev         │
│ • Win32 WM_DEVICECHANGE     │ │ • Kernel serio0 bus unbind  │
│ • Interception filter driver│ │ • GTK3 / AppIndicator3      │
│ • Task Scheduler elevation  │ │ • Native Debian package     │
└─────────────────────────────┘ └─────────────────────────────┘
```

### 1. Windows Engine Stack
* **Language & Runtime:** C# 12 compiled targeting **.NET 8 LTS** as a self-contained, single-file native executable (`win-x64`). End-users do not need to install .NET runtimes manually.
* **Kernel Scancode Filtering:** Powered by an unmanaged P/Invoke bridge to the **Interception** driver filter library (`interception.dll`). Rather than attempting to disable critical hardware devices through PnP (which Windows blocks with `CM_DEVCAP_NONDISABLEABLE`), the engine attaches an upper-class filter driver (`keyboard.sys`) directly in kernel memory. When active, scancodes originating from the internal PS/2 hardware ID (`ACPI\HPQ8002` or similar) are dropped in memory in `0.2ms`, while scancodes from external USB HID devices flow through unmodified.
* **Event-Driven Hardware Detection:** Implements a dedicated Win32 native window (`System.Windows.Forms.NativeWindow`) registering `RegisterDeviceNotification` for `GUID_DEVINTERFACE_USB_DEVICE`. It listens strictly for `WM_DEVICECHANGE` (`DBT_DEVICEARRIVAL` and `DBT_DEVICEREMOVECOMPLETE`), ensuring **0.00% idle CPU utilization** and eliminating polling loops.
* **Power Management & Shell Recovery:** 
  * Hooks `WM_POWERBROADCAST` (`PBT_APMRESUMEAUTOMATIC`) to verify dongle presence and restore keyboard states immediately when waking from sleep or hibernation.
  * Registers `RegisterWindowMessage("TaskbarCreated")` so the system tray notification icon automatically restores itself if Windows Explorer (`explorer.exe`) restarts or crashes.
* **Automated Elevation & Boot Startup:** Configured via Windows Task Scheduler (`schtasks.exe`) with `HighestAvailable` runlevel triggered `ONLOGON`. This allows the driver communication layer to execute with required administrator rights at login without displaying UAC prompts on every boot.

### 2. Linux / Ubuntu Engine Stack
* **Kernel Bus Manipulation:** Bypasses user-space workarounds by directly unbinding the internal AT keyboard from the Linux kernel `serio` bus (`/sys/bus/serio/drivers/atkbd/unbind` targeting `serio0`). This silences the internal keyboard at the kernel level across both **Wayland** and **X11** sessions with zero third-party drivers.
* **Hardware Event Pipeline:** Uses `pyudev` to stream real-time kernel `udev` subsystem events for USB device attachment and detachment.
* **Desktop Integration:** Built with GTK 3 and `AppIndicator3` (`python3-gi`) for native top-bar tray status menus in GNOME, Unity, and XFCE desktop environments.
* **Packaging & Security:** Shipped as a standard Debian package (`.deb`) with automated `postinst` permissions, XDG autostart (`/etc/xdg/autostart`), desktop launcher (`/usr/share/applications`), and a scoped `/etc/sudoers.d/laptop-keyboard-toggle` rule allowing the background helper to unbind the bus without requiring sudo password prompts.

### 3. Web & Documentation Stack
* **Landing Page:** Static HTML5, modern CSS3 with frosted-glass acrylic surfaces (`backdrop-filter: blur(14px)`), and vanilla JavaScript.
* **Design Discipline:** Adheres strictly to the **Impeccable** craft standard—high-contrast typography using **Sora** and **JetBrains Mono**, full WCAG AA contrast compliance, responsive fluid scaling (`clamp()`), and zero generic templates or artificial glow halos.
* **Automated Social Previews:** Headless Chromium automated pipeline generating pixel-perfect **1200 × 630 px** Open Graph cards (`og-image.png`).
* **Hosting:** GitHub Pages with custom routing and automated branch deployment.

### 4. CI/CD & Automation
* **GitHub Actions:** Multi-platform CI pipeline (`.github/workflows/build.yml`) that compiles the .NET 8 Windows bundle on `windows-latest`, validates the Linux `.deb` builder, packages zip archives, and publishes release binaries automatically upon git tags.

---

## 🪟 Windows Setup & Usage

### Why Special Handling is Needed on Windows
Standard Windows PnP flags internal PS/2 keyboards (`ACPI\...`) with `CM_DEVCAP_NONDISABLEABLE`. Running `pnputil /disable-device` returns:
```text
Failed to disable device:
Cannot disable critical system device.
```

To solve this, this app uses a **Dual-Engine Architecture**:
1. **Interception Kernel Filter Driver (Primary for PS/2 keyboards):** Drops keystrokes from the internal keyboard at the driver filter level in memory with 0ms latency.
2. **SetupAPI / PnP (For I2C/HID keyboards):** Toggles device power state via Windows Device Manager API.

### Windows Installation Steps
1. Download and extract **`LaptopKeyboardAutoToggle-win-x64.zip`**.
2. **One-Click Automated Setup:** Right-click **`ONE-CLICK-SETUP.bat`** and click **Run as administrator** (or double-click and allow UAC).
   * It installs the driver filter, configures the elevated logon task, and launches the app.
3. **Restart your laptop once** so the Windows kernel loads the keyboard filter. From then on, automation is 100% hands-free!

### Windows Controls & Shortcuts
* **Dashboard Window:** Click or double-click the tray icon to open the Dashboard and click **"Turn Keyboard OFF / ON"** with your mouse or touchpad.
* **Emergency Hotkey:** Press **`Ctrl + Alt + Shift + F12`** anytime to instantly restore your internal keyboard.
* **Configuration:** Edit `config.json` next to the executable to customize hardware IDs or hotkeys.

---

## 🐧 Ubuntu / Debian Linux Setup & Usage

### How It Works on Linux
On Linux, there is no "critical system device" block. The kernel natively supports unbinding the internal AT keyboard from the `serio` bus (`/sys/bus/serio/drivers/atkbd/unbind`). This silences the hardware at the kernel level across both **Wayland** and **X11** with zero third-party drivers.

### Linux Installation Steps
1. Download **`laptop-keyboard-auto-toggle_1.1-1_all.deb`**.
2. Install via Terminal (`apt` pulls in the Python/GTK dependencies):
   ```bash
   sudo apt install ./laptop-keyboard-auto-toggle_1.1-1_all.deb
   ```
3. Start it once from the app menu (**Laptop Keyboard Auto-Toggle**), or log out and back in. It auto-starts on every login after that (`/etc/xdg/autostart/`).
4. **Pick your dongle:** click the keyboard icon in the top bar → **Choose Wireless Dongle** → select your receiver. The choice is saved, and the internal keyboard turns off right away (unbinding can take a few seconds).

> On Debian or non-Ubuntu GNOME, the top-bar icon needs the AppIndicator extension (`gnome-shell-extension-appindicator`). Ubuntu ships it by default.

### Linux Controls & Configuration
* **Top Bar Tray Icon:** Shows live status (`Internal KB: OFF | Dongle: PLUGGED`), lets you choose the dongle, pause auto-toggling, and manually switch the keyboard ON/OFF.
* **Uninstall:** `sudo apt remove laptop-keyboard-auto-toggle` stops the app and re-enables the internal keyboard.
* **Configuration:** The dongle menu writes `~/.config/laptop-keyboard-auto-toggle/config.json`; you can also edit it by hand (IDs from `lsusb`):
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
├── .github/workflows/build.yml   # Automated GitHub Actions build & release CI
├── docs/                         # Landing page, simulator & social preview
│   ├── index.html                # Responsive landing page
│   ├── style.css                 # Impeccable CSS design system
│   ├── script.js                 # Interactive hardware simulator
│   ├── social-preview.html       # 1200x630 Open Graph card layout
│   └── og-image.png              # Rendered social preview image
├── MainForm.cs                   # Windows Dashboard UI window
├── MainForm.Designer.cs          # UI component layout & controls
├── TrayApplicationContext.cs     # Windows system tray & Win32 event listener
├── DeviceManagerService.cs       # Windows state machine & hardware detection
├── InterceptionFilter.cs         # Windows Interception driver C# P/Invoke wrapper
├── ONE-CLICK-SETUP.bat           # 1-click elevated driver & task installer
├── Install-Task.ps1              # Windows Task Scheduler automation script
├── Uninstall-Task.ps1            # Windows uninstaller script
├── Setup-Interception.ps1        # Windows driver installer script
├── linux/                        # Linux packaging source
│   ├── build_deb.py              # Builds the .deb without dpkg (e.g. from Windows)
│   └── package/                  # .deb root (version lives in DEBIAN/control)
│       ├── DEBIAN/control        # Debian package metadata
│       ├── usr/bin/              # Python daemon & serio helper script
│       └── etc/                  # Sudoers permissions & autostart entry
├── PRODUCT.md                    # Product specification schema
└── LICENSE                       # MIT License
```

---

## 📄 License
This project is licensed under the [MIT License](LICENSE). Built with craft by **Azizullah Tanim**.
