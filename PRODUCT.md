# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Stack

Static HTML5, modern CSS3, and vanilla JavaScript (optimized for instant loading and zero-dependency GitHub Pages hosting).

## Users

Laptop owners (on Windows 10/11 and Ubuntu/Debian) dealing with malfunctioning, sticky, broken, or water-damaged internal laptop keyboards who use external wireless or USB keyboards and need the internal keyboard reliably silenced without hardware surgery.

## Product Purpose

LaptopKeyboardAutoToggle provides seamless, automatic hardware-level suppression of internal laptop keyboards when an external wireless keyboard dongle is plugged in, restores typing capability immediately when unplugged, and gives users a quick manual override dashboard.

## Positioning

Unlike typical device managers that fail on internal laptop keyboards with "Cannot disable critical system device" (because Windows marks PS/2 keyboards as `CM_DEVCAP_NONDISABLEABLE`), LaptopKeyboardAutoToggle solves this through an intelligent Dual-Engine Architecture:
1. Kernel filter driver (Interception) for instant, 0-latency keystroke suppression on Windows.
2. Direct Linux `serio` bus unbinding for Ubuntu/Debian.

## Operating Context

- Desktop system tray / top-bar indicator in Windows and Ubuntu.
- Automatic logon startup via Windows Task Scheduler (elevated, no UAC popups).
- Distributed via GitHub Releases as a single-file Windows executable bundle and a native Ubuntu `.deb` package.

## Capabilities and Constraints

- Event-driven USB detection (`WM_DEVICECHANGE` & Linux `udev`), strictly no tight polling loops.
- Emergency safety overrides: `Ctrl + Alt + Shift + F12` and mouse-clickable tray restore.
- Sleep/resume awareness: restores keyboard if PC wakes up without the dongle.
- 1-click installer: `ONE-CLICK-SETUP.bat` and `.deb` package.

## Brand Commitments

- Open source, MIT licensed.
- 100% local execution, zero network telemetry, privacy-first.
- Clean, focused engineering with robust failure recovery.

## Evidence on Hand

- Live repository: `https://github.com/tanim913/LaptopKeyboardAutoToggle`
- Releases available: `v1.0.0` with `LaptopKeyboardAutoToggle-win-x64.zip` and `laptop-keyboard-auto-toggle_1.0-1_all.deb`.
- Verified hardware compatibility: HP PS/2 keyboards (`ACPI\HPQ8002`), generic wireless combos (`VID_1EA7&PID_0066`).

## Product Principles

1. **Safety First**: The internal keyboard must never be permanently locked out; multiple failsafes (hotkey, tray, exit handler, sleep wake) guarantee recovery.
2. **Zero Friction**: One or two clicks to install and run.
3. **Respect System Resources**: Pure event-driven hooks, zero idle CPU usage.
