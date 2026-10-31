# Changelog

## v1.2.0 — Ubuntu / Debian fixes (2026-10-31)

The Windows app is unchanged. The Linux `.deb` from v1.0.0 installed fine but did not work on a real Ubuntu laptop. Tested on Ubuntu 20.04 (GNOME, X11) with a SHARKOON wireless keyboard dongle (`1ea7:0066`), and in CI on Ubuntu 22.04 and 24.04.

### What was broken, and the fix

| Problem | Cause | Fix |
|---|---|---|
| Plugging the dongle did nothing on most laptops | The dongle ID was hard-coded to `1ea7:0066`; any other receiver was silently ignored | **Choose Wireless Dongle** menu in the tray icon, or `sudo laptop-keyboard-toggle-helper set-dongle vvvv:pppp` |
| Laptop keyboard came back on by itself seconds after being disabled | The kernel re-attaches `atkbd` to an unbound serio port on the next byte the keyboard sends (any keypress, or ghost keystrokes from a faulty keyboard) | The port is switched to `bind_mode=manual` while disabled and back to `auto` when enabled (a reboot always resets it) |
| Unplugging the dongle left **no working keyboard** | udev removal events don't carry `ID_VENDOR_ID`/`ID_MODEL_ID`, so the unplug was never recognised | Match on the kernel's `PRODUCT` field, which is present on both plug and unplug |
| Nothing happened until the next login, nor at the login screen | Switching was done by the tray app, which only starts after login | A udev rule (`/lib/udev/rules.d/90-laptop-keyboard-auto-toggle.rules`) switches on every USB plug/unplug and at boot, so it works before login and without the tray app |
| A logout could leave the keyboard disabled | Signal handlers were installed only after the first unbind, which takes ~4 s | Superseded: the tray app no longer switches anything; udev keeps the state correct |
| Two tray icons fighting each other | Autostart and the app-menu launcher could both run | Single-instance lock |

### Other changes

- The dongle setting is now system-wide (`/etc/laptop-keyboard-auto-toggle.conf`) so it applies at the login screen. The old per-user `~/.config/laptop-keyboard-auto-toggle/config.json` is no longer used.
- The tray icon is status and control only (choose dongle, pause, manual ON/OFF). Quitting it does not stop auto-toggling.
- The root helper (callable by any user through sudoers) validates every argument.
- Installing applies the current dongle state immediately; removing re-enables the internal keyboard and the udev rule; purging deletes the config.
- Package root moved to `linux/package/`; the version lives only in `DEBIAN/control`.
- CI lints the package, then installs, exercises and removes/purges it on Ubuntu 22.04 and 24.04 before a release is published.

### Upgrading

```bash
sudo apt install ./laptop-keyboard-auto-toggle_1.2.0-1_all.deb
```

Works over v1.0.0 in place. v1.1.0 was withdrawn because it could leave you with no working keyboard after unplugging the dongle; if you installed it, upgrade.

## v1.0.0

Initial release: Windows (Interception driver, tray dashboard, Task Scheduler autostart) and the first Ubuntu/Debian package.
