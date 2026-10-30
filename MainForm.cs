using System;
using System.Drawing;
using System.Windows.Forms;

namespace LaptopKeyboardAutoToggle
{
    public class MainForm : Form
    {
        private readonly DeviceManagerService _deviceService;
        private readonly AppConfig _config;

        private Label _lblStatusTitle = null!;
        private Label _lblKeyboardStatus = null!;
        private Label _lblDongleStatus = null!;
        private Label _lblEngineStatus = null!;
        private Button _btnManualToggle = null!;
        private CheckBox _chkAutoToggle = null!;
        private Button _btnOpenLogs = null!;
        private Button _btnReloadConfig = null!;
        private Label _lblEmergencyHotkey = null!;
        private GroupBox _grpStatus = null!;
        private GroupBox _grpControls = null!;

        public MainForm(DeviceManagerService deviceService, AppConfig config)
        {
            _deviceService = deviceService;
            _config = config;

            InitializeComponent();
            UpdateUi();

            _deviceService.StateChanged += () =>
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(UpdateUi));
                }
                else
                {
                    UpdateUi();
                }
            };
        }

        private void InitializeComponent()
        {
            Text = "Laptop Keyboard Auto-Toggle & Manual Switch";
            Size = new Size(540, 420);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // Group 1: Live Status
            _grpStatus = new GroupBox
            {
                Text = " Device & Automation Status ",
                Location = new Point(20, 15),
                Size = new Size(485, 175),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            _lblKeyboardStatus = new Label
            {
                Location = new Point(20, 30),
                Size = new Size(445, 30),
                Font = new Font("Segoe UI Semibold", 11.5f),
                Text = "Internal Keyboard: Checking..."
            };

            _lblDongleStatus = new Label
            {
                Location = new Point(20, 65),
                Size = new Size(445, 25),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Text = $"USB Dongle ({_config.UsbDongleHardwareId}): Checking..."
            };

            _lblEngineStatus = new Label
            {
                Location = new Point(20, 95),
                Size = new Size(445, 25),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.DarkSlateGray,
                Text = "Engine: Interception Filter / SetupAPI PnP"
            };

            _lblEmergencyHotkey = new Label
            {
                Location = new Point(20, 125),
                Size = new Size(445, 35),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.DarkRed,
                Text = $"Emergency Hotkey: {_config.EmergencyHotkey} (Instantly re-enables keyboard)"
            };

            _grpStatus.Controls.Add(_lblKeyboardStatus);
            _grpStatus.Controls.Add(_lblDongleStatus);
            _grpStatus.Controls.Add(_lblEngineStatus);
            _grpStatus.Controls.Add(_lblEmergencyHotkey);

            // Group 2: Controls & Overrides
            _grpControls = new GroupBox
            {
                Text = " Manual Control & Settings ",
                Location = new Point(20, 205),
                Size = new Size(485, 155),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            _btnManualToggle = new Button
            {
                Location = new Point(20, 30),
                Size = new Size(240, 45),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System
            };
            _btnManualToggle.Click += OnManualToggleClicked;

            _chkAutoToggle = new CheckBox
            {
                Location = new Point(20, 85),
                Size = new Size(445, 25),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Text = "Enable Automatic Toggle when USB Dongle is plugged / unplugged",
                Checked = !_deviceService.IsAutomationPaused
            };
            _chkAutoToggle.CheckedChanged += (s, e) =>
            {
                _deviceService.IsAutomationPaused = !_chkAutoToggle.Checked;
                Logger.Info($"Automation toggled via UI: Paused={_deviceService.IsAutomationPaused}");
                UpdateUi();
            };

            _btnOpenLogs = new Button
            {
                Location = new Point(275, 30),
                Size = new Size(95, 45),
                Font = new Font("Segoe UI", 9f),
                Text = "Open Logs",
                FlatStyle = FlatStyle.System
            };
            _btnOpenLogs.Click += (s, e) => Logger.OpenLogFile();

            _btnReloadConfig = new Button
            {
                Location = new Point(375, 30),
                Size = new Size(90, 45),
                Font = new Font("Segoe UI", 9f),
                Text = "Reload Config",
                FlatStyle = FlatStyle.System
            };
            _btnReloadConfig.Click += (s, e) =>
            {
                var cfg = AppConfig.Load();
                MessageBox.Show(this, $"Config reloaded!\nInternal KB: {cfg.InternalKeyboardHardwareId}\nDongle: {cfg.UsbDongleHardwareId}", "Config", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _deviceService.InitializeState();
            };

            _grpControls.Controls.Add(_btnManualToggle);
            _grpControls.Controls.Add(_chkAutoToggle);
            _grpControls.Controls.Add(_btnOpenLogs);
            _grpControls.Controls.Add(_btnReloadConfig);

            Controls.Add(_grpStatus);
            Controls.Add(_grpControls);

            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide(); // Minimize to system tray
                }
            };
        }

        private void OnManualToggleClicked(object? sender, EventArgs e)
        {
            if (_deviceService.IsInternalKeyboardDisabled)
            {
                Logger.Info("User manually clicked: Turn Keyboard ON.");
                _deviceService.EnableInternalKeyboard();
            }
            else
            {
                Logger.Info("User manually clicked: Turn Keyboard OFF.");
                _deviceService.DisableInternalKeyboard();
            }
            UpdateUi();
        }

        public void UpdateUi()
        {
            if (_deviceService.IsInternalKeyboardDisabled)
            {
                _lblKeyboardStatus.Text = "Internal Keyboard: DISABLED (Turned OFF)";
                _lblKeyboardStatus.ForeColor = Color.Red;
                _btnManualToggle.Text = "Turn Keyboard ON";
            }
            else
            {
                _lblKeyboardStatus.Text = "Internal Keyboard: ENABLED (Active)";
                _lblKeyboardStatus.ForeColor = Color.DarkGreen;
                _btnManualToggle.Text = "Turn Keyboard OFF";
            }

            if (_deviceService.IsDongleConnected)
            {
                _lblDongleStatus.Text = $"USB Dongle: CONNECTED ({_config.UsbDongleHardwareId})";
                _lblDongleStatus.ForeColor = Color.DarkBlue;
            }
            else
            {
                _lblDongleStatus.Text = $"USB Dongle: DISCONNECTED ({_config.UsbDongleHardwareId})";
                _lblDongleStatus.ForeColor = Color.Gray;
            }

            _chkAutoToggle.Checked = !_deviceService.IsAutomationPaused;
        }
    }
}

