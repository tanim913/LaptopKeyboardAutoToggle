using System;
using System.Threading;
using System.Windows.Forms;

namespace LaptopKeyboardAutoToggle
{
    internal static class Program
    {
        private const string MutexName = "Global\\LaptopKeyboardAutoToggle_SingleInstance_Mutex";
        private static Mutex? _singleInstanceMutex;
        private static DeviceManagerService? _deviceService;

        [STAThread]
        static void Main()
        {
            // 1. Enforce Single-Instance
            _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("LaptopKeyboardAutoToggle is already running in the system tray.", "Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. Load Configuration
            var config = AppConfig.Load();
            Logger.Info("=================================================");
            Logger.Info("LaptopKeyboardAutoToggle Starting Up...");
            Logger.Info($"Target Internal Keyboard: {config.InternalKeyboardHardwareId}");
            Logger.Info($"Target USB Dongle: {config.UsbDongleHardwareId}");
            Logger.Info("=================================================");

            _deviceService = new DeviceManagerService(config);

            // 3. Register Global Crash & Exit Handlers for Fail-Safe Recovery
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Logger.Error($"Unhandled domain exception: {e.ExceptionObject}");
                EnsureFailSafeRestore();
            };

            Application.ThreadException += (s, e) =>
            {
                Logger.Error($"Application thread exception: {e.Exception.Message}");
                EnsureFailSafeRestore();
            };

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                Logger.Info("Process exit event received. Running fail-safe keyboard restoration...");
                EnsureFailSafeRestore();
            };

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using (var trayContext = new TrayApplicationContext(_deviceService, config))
                {
                    Application.Run(trayContext);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Fatal exception in Main loop: {ex}");
                EnsureFailSafeRestore();
            }
            finally
            {
                EnsureFailSafeRestore();
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
                Logger.Info("Application terminated cleanly.");
            }
        }

        private static void EnsureFailSafeRestore()
        {
            try
            {
                if (_deviceService != null)
                {
                    _deviceService.EnableInternalKeyboard();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed during fail-safe restore: {ex.Message}");
            }
        }
    }
}

