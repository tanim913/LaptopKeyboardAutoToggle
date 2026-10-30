using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LaptopKeyboardAutoToggle
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly DeviceManagerService _deviceService;
        private readonly AppConfig _config;
        private readonly MainForm _mainForm;
        private readonly DeviceNotificationWindow _msgWindow;

        private ToolStripMenuItem _statusMenuItem = null!;
        private ToolStripMenuItem _manualToggleMenuItem = null!;
        private ToolStripMenuItem _pauseAutoMenuItem = null!;
        private ToolStripMenuItem _safetyEnableMenuItem = null!;
        private ToolStripMenuItem _openUiMenuItem = null!;
        private ToolStripMenuItem _openLogMenuItem = null!;
        private ToolStripMenuItem _exitMenuItem = null!;

        public TrayApplicationContext(DeviceManagerService deviceService, AppConfig config)
        {
            _deviceService = deviceService;
            _config = config;
            _mainForm = new MainForm(_deviceService, _config);

            // Initialize tray menu
            var contextMenu = new ContextMenuStrip();

            _statusMenuItem = new ToolStripMenuItem("Status: Initializing...") { Enabled = false };
            _manualToggleMenuItem = new ToolStripMenuItem("Toggle Keyboard State", null, OnManualToggleClicked);
            _pauseAutoMenuItem = new ToolStripMenuItem("Pause USB Automation", null, OnPauseAutoClicked) { CheckOnClick = true };
            _safetyEnableMenuItem = new ToolStripMenuItem("Enable internal keyboard now", null, (s, e) => _deviceService.EnableInternalKeyboard());
            _openUiMenuItem = new ToolStripMenuItem("Show Dashboard...", null, (s, e) => ShowMainForm());
            _openLogMenuItem = new ToolStripMenuItem("Open Log File", null, (s, e) => Logger.OpenLogFile());
            _exitMenuItem = new ToolStripMenuItem("Exit", null, OnExitClicked);

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                _statusMenuItem,
                new ToolStripSeparator(),
                _manualToggleMenuItem,
                _pauseAutoMenuItem,
                _safetyEnableMenuItem,
                new ToolStripSeparator(),
                _openUiMenuItem,
                _openLogMenuItem,
                new ToolStripSeparator(),
                _exitMenuItem
            });

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = contextMenu,
                Text = "Laptop Keyboard Auto-Toggle",
                Visible = true
            };

            _trayIcon.DoubleClick += (s, e) => ShowMainForm();
            _trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowMainForm();
                }
            };

            // Hidden message window for device events & hotkeys
            _msgWindow = new DeviceNotificationWindow(_deviceService, _config, this);

            _deviceService.StateChanged += UpdateTrayUi;
            UpdateTrayUi();

            // Run initial device scan
            _deviceService.InitializeState();
        }

        private void ShowMainForm()
        {
            if (_mainForm.Visible)
            {
                _mainForm.WindowState = FormWindowState.Normal;
                _mainForm.Activate();
            }
            else
            {
                _mainForm.Show();
                _mainForm.WindowState = FormWindowState.Normal;
                _mainForm.Activate();
            }
        }

        private void OnManualToggleClicked(object? sender, EventArgs e)
        {
            if (_deviceService.IsInternalKeyboardDisabled)
            {
                _deviceService.EnableInternalKeyboard();
            }
            else
            {
                _deviceService.DisableInternalKeyboard();
            }
        }

        private void OnPauseAutoClicked(object? sender, EventArgs e)
        {
            _deviceService.IsAutomationPaused = _pauseAutoMenuItem.Checked;
            Logger.Info($"Automation Pause set to: {_deviceService.IsAutomationPaused}");
            UpdateTrayUi();
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            Logger.Info("Exit requested from tray menu. Restoring keyboard and cleaning up...");
            _deviceService.EnableInternalKeyboard();
            ExitThread();
        }

        public void RefreshTrayIcon()
        {
            try
            {
                _trayIcon.Visible = false;
                _trayIcon.Visible = true;
            }
            catch { }
        }

        public void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try
            {
                _trayIcon.ShowBalloonTip(3000, title, text, icon);
            }
            catch { }
        }

        private void UpdateTrayUi()
        {
            string kbText = _deviceService.IsInternalKeyboardDisabled ? "Internal KB: OFF" : "Internal KB: ON";
            string dongleText = _deviceService.IsDongleConnected ? "Dongle: PLUGGED" : "Dongle: UNPLUGGED";
            _statusMenuItem.Text = $"{kbText} | {dongleText}";

            _manualToggleMenuItem.Text = _deviceService.IsInternalKeyboardDisabled ? "Turn Internal Keyboard ON" : "Turn Internal Keyboard OFF";
            _pauseAutoMenuItem.Checked = _deviceService.IsAutomationPaused;

            string tooltip = $"AutoToggle: {kbText} | {dongleText}";
            if (tooltip.Length >= 64) tooltip = tooltip.Substring(0, 63);
            _trayIcon.Text = tooltip;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _msgWindow.Dispose();
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _mainForm.Dispose();
            }
            base.Dispose(disposing);
        }

        // Nested Window for Win32 Message Pump
        private class DeviceNotificationWindow : NativeWindow, IDisposable
        {
            private readonly DeviceManagerService _deviceService;
            private readonly AppConfig _config;
            private readonly TrayApplicationContext _parent;
            private IntPtr _hDeviceNotify = IntPtr.Zero;
            private const int HOTKEY_ID = 9001;
            private readonly uint _wmTaskbarCreated;

            public DeviceNotificationWindow(DeviceManagerService deviceService, AppConfig config, TrayApplicationContext parent)
            {
                _deviceService = deviceService;
                _config = config;
                _parent = parent;

                CreateHandle(new CreateParams());

                _wmTaskbarCreated = NativeMethods.RegisterWindowMessage("TaskbarCreated");
                RegisterDeviceEvents();
                RegisterEmergencyHotkey();
            }

            private void RegisterDeviceEvents()
            {
                try
                {
                    var di = new NativeMethods.DEV_BROADCAST_DEVICEINTERFACE
                    {
                        dbcc_size = Marshal.SizeOf(typeof(NativeMethods.DEV_BROADCAST_DEVICEINTERFACE)),
                        dbcc_devicetype = NativeMethods.DBT_DEVTYP_DEVICEINTERFACE,
                        dbcc_classguid = NativeMethods.GUID_DEVINTERFACE_USB_DEVICE
                    };

                    IntPtr buffer = Marshal.AllocHGlobal(di.dbcc_size);
                    Marshal.StructureToPtr(di, buffer, true);

                    _hDeviceNotify = NativeMethods.RegisterDeviceNotification(
                        Handle,
                        buffer,
                        NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE | NativeMethods.DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);

                    Marshal.FreeHGlobal(buffer);

                    if (_hDeviceNotify == IntPtr.Zero)
                    {
                        Logger.Warn($"RegisterDeviceNotification returned null (Error: 0x{Marshal.GetLastWin32Error():X8})");
                    }
                    else
                    {
                        Logger.Info("Win32 USB & HID Device arrival/removal notification registered successfully.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to register device notification: {ex.Message}");
                }
            }

            private void RegisterEmergencyHotkey()
            {
                try
                {
                    // Hotkey: Ctrl + Alt + Shift + F12
                    uint modifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT;
                    uint vk = (uint)Keys.F12;

                    if (NativeMethods.RegisterHotKey(Handle, HOTKEY_ID, modifiers, vk))
                    {
                        Logger.Info($"Emergency global hotkey registered: {_config.EmergencyHotkey}");
                    }
                    else
                    {
                        Logger.Warn($"Failed to register emergency hotkey (Error 0x{Marshal.GetLastWin32Error():X8})");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error registering hotkey: {ex.Message}");
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == _wmTaskbarCreated && _wmTaskbarCreated != 0)
                {
                    Logger.Info("Windows Explorer TaskbarCreated message received. Refreshing tray icon.");
                    _parent.RefreshTrayIcon();
                }

                switch (m.Msg)
                {
                    case NativeMethods.WM_DEVICECHANGE:
                        int eventType = m.WParam.ToInt32();
                        if (eventType == NativeMethods.DBT_DEVICEARRIVAL || eventType == NativeMethods.DBT_DEVICEREMOVECOMPLETE)
                        {
                            string? devPath = null;
                            if (m.LParam != IntPtr.Zero)
                            {
                                try
                                {
                                    var hdr = Marshal.PtrToStructure<NativeMethods.DEV_BROADCAST_HDR>(m.LParam);
                                    if (hdr.dbch_devicetype == NativeMethods.DBT_DEVTYP_DEVICEINTERFACE)
                                    {
                                        var di = Marshal.PtrToStructure<NativeMethods.DEV_BROADCAST_DEVICEINTERFACE>(m.LParam);
                                        devPath = di.dbcc_name;
                                    }
                                }
                                catch { }
                            }

                            if (eventType == NativeMethods.DBT_DEVICEARRIVAL)
                            {
                                _deviceService.OnDeviceArrived(devPath);
                            }
                            else
                            {
                                _deviceService.OnDeviceRemoved(devPath);
                            }
                        }
                        break;

                    case NativeMethods.WM_POWERBROADCAST:
                        int powerEvent = m.WParam.ToInt32();
                        if (powerEvent == NativeMethods.PBT_APMRESUMEAUTOMATIC || powerEvent == NativeMethods.PBT_APMRESUMESUSPEND)
                        {
                            _deviceService.OnSystemResumed();
                        }
                        break;

                    case NativeMethods.WM_HOTKEY:
                        if (m.WParam.ToInt32() == HOTKEY_ID)
                        {
                            Logger.Info("[EMERGENCY HOTKEY TRIGGERED] Restoring internal keyboard immediately!");
                            _deviceService.EnableInternalKeyboard();
                            _parent.ShowBalloon("Emergency Hotkey", "Internal keyboard has been forcibly ENABLED!", ToolTipIcon.Warning);
                        }
                        break;
                }

                base.WndProc(ref m);
            }

            public void Dispose()
            {
                if (_hDeviceNotify != IntPtr.Zero)
                {
                    NativeMethods.UnregisterDeviceNotification(_hDeviceNotify);
                    _hDeviceNotify = IntPtr.Zero;
                }

                NativeMethods.UnregisterHotKey(Handle, HOTKEY_ID);
                DestroyHandle();
            }
        }
    }
}

