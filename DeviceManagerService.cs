using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LaptopKeyboardAutoToggle
{
    public class DeviceManagerService : IDisposable
    {
        private readonly AppConfig _config;
        private readonly object _stateLock = new object();
        private readonly InterceptionFilter _interceptionFilter;

        public bool IsInternalKeyboardDisabled { get; private set; } = false;
        public bool IsDongleConnected { get; private set; } = false;
        public bool IsAutomationPaused { get; set; } = false;
        public bool IsInterceptionActive => _interceptionFilter.IsDriverAvailable;

        public event Action? StateChanged;

        public DeviceManagerService(AppConfig config)
        {
            _config = config;
            _interceptionFilter = new InterceptionFilter(_config.InternalKeyboardHardwareId);
            _interceptionFilter.TryInitialize();
        }

        public void InitializeState()
        {
            lock (_stateLock)
            {
                Logger.Info("Initializing device presence state...");
                IsDongleConnected = CheckIfDonglePresent();
                Logger.Info($"Initial scan: USB Dongle present = {IsDongleConnected}");

                if (!IsAutomationPaused && IsDongleConnected)
                {
                    Logger.Info("Dongle is plugged in at startup: turning internal keyboard OFF.");
                    DisableInternalKeyboard();
                }
                else
                {
                    Logger.Info("Dongle is not plugged in at startup: ensuring internal keyboard is ON.");
                    EnableInternalKeyboard();
                }

                StateChanged?.Invoke();
            }
        }

        public void OnDeviceArrived(string? devicePath)
        {
            lock (_stateLock)
            {
                if (IsAutomationPaused)
                {
                    Logger.Info("Device arrival ignored: Automation is paused.");
                    return;
                }

                bool matches = false;
                if (!string.IsNullOrEmpty(devicePath) && devicePath.Contains(_config.UsbDongleHardwareId, StringComparison.OrdinalIgnoreCase))
                {
                    matches = true;
                }
                else
                {
                    matches = CheckIfDonglePresent();
                }

                if (matches)
                {
                    Logger.Info($"USB Dongle ARRIVAL detected! (Path: {devicePath ?? "N/A"})");
                    IsDongleConnected = true;
                    DisableInternalKeyboard();
                    StateChanged?.Invoke();
                }
            }
        }

        public void OnDeviceRemoved(string? devicePath)
        {
            lock (_stateLock)
            {
                if (IsAutomationPaused)
                {
                    Logger.Info("Device removal ignored: Automation is paused.");
                    return;
                }

                bool matches = false;
                if (!string.IsNullOrEmpty(devicePath) && devicePath.Contains(_config.UsbDongleHardwareId, StringComparison.OrdinalIgnoreCase))
                {
                    matches = true;
                }
                else
                {
                    matches = !CheckIfDonglePresent();
                }

                if (matches)
                {
                    Logger.Info($"USB Dongle REMOVAL detected! (Path: {devicePath ?? "N/A"})");
                    IsDongleConnected = false;
                    EnableInternalKeyboard();
                    StateChanged?.Invoke();
                }
            }
        }

        public void OnSystemResumed()
        {
            lock (_stateLock)
            {
                Logger.Info("System resumed from sleep/hibernate. Verifying dongle status...");
                IsDongleConnected = CheckIfDonglePresent();
                Logger.Info($"Post-resume verification: Dongle present = {IsDongleConnected}");

                if (!IsDongleConnected)
                {
                    Logger.Info("Safety check: Dongle is not present after resume. Ensuring internal keyboard is ENABLED.");
                    EnableInternalKeyboard();
                }
                else if (!IsAutomationPaused)
                {
                    Logger.Info("Dongle is present after resume. Ensuring internal keyboard is DISABLED.");
                    DisableInternalKeyboard();
                }

                StateChanged?.Invoke();
            }
        }

        public bool CheckIfDonglePresent()
        {
            try
            {
                Guid usbGuid = NativeMethods.GUID_DEVINTERFACE_USB_DEVICE;
                IntPtr hDevInfo = NativeMethods.SetupDiGetClassDevs(ref usbGuid, IntPtr.Zero, IntPtr.Zero, NativeMethods.DIGCF_PRESENT | NativeMethods.DIGCF_DEVICEINTERFACE);
                if (hDevInfo != IntPtr.Zero && hDevInfo != new IntPtr(-1))
                {
                    try
                    {
                        var devInfoData = new NativeMethods.SP_DEVINFO_DATA();
                        devInfoData.cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.SP_DEVINFO_DATA));
                        uint index = 0;

                        while (NativeMethods.SetupDiEnumDeviceInfo(hDevInfo, index, ref devInfoData))
                        {
                            index++;
                            string hardwareId = GetDeviceHardwareId(hDevInfo, ref devInfoData);
                            string instanceId = GetDeviceInstanceId(hDevInfo, ref devInfoData);

                            if (hardwareId.Contains(_config.UsbDongleHardwareId, StringComparison.OrdinalIgnoreCase) ||
                                instanceId.Contains(_config.UsbDongleHardwareId, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                    finally
                    {
                        NativeMethods.SetupDiDestroyDeviceInfoList(hDevInfo);
                    }
                }

                // Fallback: Check all present devices
                IntPtr hAllDevs = NativeMethods.SetupDiGetClassDevs(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, NativeMethods.DIGCF_PRESENT | NativeMethods.DIGCF_ALLCLASSES);
                if (hAllDevs != IntPtr.Zero && hAllDevs != new IntPtr(-1))
                {
                    try
                    {
                        var devInfoData = new NativeMethods.SP_DEVINFO_DATA();
                        devInfoData.cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.SP_DEVINFO_DATA));
                        uint index = 0;

                        while (NativeMethods.SetupDiEnumDeviceInfo(hAllDevs, index, ref devInfoData))
                        {
                            index++;
                            string hardwareId = GetDeviceHardwareId(hAllDevs, ref devInfoData);
                            string instanceId = GetDeviceInstanceId(hAllDevs, ref devInfoData);

                            if (hardwareId.Contains(_config.UsbDongleHardwareId, StringComparison.OrdinalIgnoreCase) ||
                                instanceId.Contains(_config.UsbDongleHardwareId, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                    finally
                    {
                        NativeMethods.SetupDiDestroyDeviceInfoList(hAllDevs);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking dongle presence: {ex.Message}");
            }

            return false;
        }

        public bool DisableInternalKeyboard()
        {
            Logger.Info($"[ACTION] Disabling internal keyboard (Target: {_config.InternalKeyboardHardwareId})...");

            // 1. If Interception filter is available, engage key suppression immediately
            if (_interceptionFilter.IsDriverAvailable)
            {
                _interceptionFilter.SuppressInternalKeyboard = true;
                Logger.Info("[INTERCEPTION] Internal keyboard key suppression ENGAGED.");
            }

            // 2. Also attempt SetupAPI / pnputil disable
            bool pnpSuccess = ChangeDeviceState(_config.InternalKeyboardHardwareId, NativeMethods.DICS_DISABLE);

            IsInternalKeyboardDisabled = true;
            Logger.Info($"[STATUS] Internal keyboard is now OFF. (Interception: {_interceptionFilter.IsDriverAvailable}, PnP: {pnpSuccess})");
            StateChanged?.Invoke();
            return true;
        }

        public bool EnableInternalKeyboard()
        {
            Logger.Info($"[ACTION] Enabling internal keyboard (Target: {_config.InternalKeyboardHardwareId})...");

            // 1. Release Interception suppression
            if (_interceptionFilter.IsDriverAvailable)
            {
                _interceptionFilter.SuppressInternalKeyboard = false;
                Logger.Info("[INTERCEPTION] Internal keyboard key suppression RELEASED.");
            }

            // 2. Attempt SetupAPI / pnputil enable
            bool pnpSuccess = ChangeDeviceState(_config.InternalKeyboardHardwareId, NativeMethods.DICS_ENABLE);

            IsInternalKeyboardDisabled = false;
            Logger.Info($"[STATUS] Internal keyboard is now ON. (Interception: {_interceptionFilter.IsDriverAvailable}, PnP: {pnpSuccess})");
            StateChanged?.Invoke();
            return true;
        }

        private bool ChangeDeviceState(string hardwareOrInstanceId, uint stateChange)
        {
            string actionName = stateChange == NativeMethods.DICS_DISABLE ? "DISABLE" : "ENABLE";
            try
            {
                Guid kbdGuid = NativeMethods.GUID_DEVCLASS_KEYBOARD;
                IntPtr hDevInfo = NativeMethods.SetupDiGetClassDevs(ref kbdGuid, IntPtr.Zero, IntPtr.Zero, NativeMethods.DIGCF_PRESENT);
                if (hDevInfo == IntPtr.Zero || hDevInfo == new IntPtr(-1))
                {
                    hDevInfo = NativeMethods.SetupDiGetClassDevs(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, NativeMethods.DIGCF_PRESENT | NativeMethods.DIGCF_ALLCLASSES);
                }

                if (hDevInfo != IntPtr.Zero && hDevInfo != new IntPtr(-1))
                {
                    try
                    {
                        var devInfoData = new NativeMethods.SP_DEVINFO_DATA();
                        devInfoData.cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.SP_DEVINFO_DATA));
                        uint index = 0;

                        while (NativeMethods.SetupDiEnumDeviceInfo(hDevInfo, index, ref devInfoData))
                        {
                            index++;
                            string hwId = GetDeviceHardwareId(hDevInfo, ref devInfoData);
                            string instId = GetDeviceInstanceId(hDevInfo, ref devInfoData);

                            if (hwId.Contains(hardwareOrInstanceId, StringComparison.OrdinalIgnoreCase) ||
                                instId.Contains(hardwareOrInstanceId, StringComparison.OrdinalIgnoreCase))
                            {
                                var propChange = new NativeMethods.SP_PROPCHANGE_PARAMS
                                {
                                    ClassInstallHeader = new NativeMethods.SP_CLASSINSTALL_HEADER
                                    {
                                        cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.SP_CLASSINSTALL_HEADER)),
                                        InstallFunction = NativeMethods.DIF_PROPERTYCHANGE
                                    },
                                    StateChange = stateChange,
                                    Scope = NativeMethods.DICS_FLAG_GLOBAL,
                                    HwProfile = 0
                                };

                                if (NativeMethods.SetupDiSetClassInstallParams(hDevInfo, ref devInfoData, ref propChange, (uint)Marshal.SizeOf(propChange)))
                                {
                                    if (NativeMethods.SetupDiCallClassInstaller(NativeMethods.DIF_PROPERTYCHANGE, hDevInfo, ref devInfoData))
                                    {
                                        Logger.Info($"SetupDiCallClassInstaller({actionName}) succeeded for {instId}");
                                        return true;
                                    }
                                }

                                int err = Marshal.GetLastWin32Error();
                                Logger.Warn($"SetupAPI {actionName} returned error 0x{err:X8}. Trying pnputil for '{instId}'...");

                                if (RunPnpUtil(instId, stateChange == NativeMethods.DICS_DISABLE))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    finally
                    {
                        NativeMethods.SetupDiDestroyDeviceInfoList(hDevInfo);
                    }
                }

                return RunPnpUtil(hardwareOrInstanceId, stateChange == NativeMethods.DICS_DISABLE);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception during ChangeDeviceState ({actionName}): {ex.Message}");
                return false;
            }
        }

        private bool RunPnpUtil(string instanceId, bool disable)
        {
            string switchCmd = disable ? "/disable-device" : "/enable-device";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "pnputil.exe",
                    Arguments = $"{switchCmd} \"{instanceId}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return false;

                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit(5000);

                string combined = (stdout + " " + stderr).Trim();
                Logger.Info($"pnputil {switchCmd} result: {combined}");

                if (proc.ExitCode == 0 && !combined.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"pnputil execution failed: {ex.Message}");
            }
            return false;
        }

        private string GetDeviceHardwareId(IntPtr hDevInfo, ref NativeMethods.SP_DEVINFO_DATA devInfoData)
        {
            byte[] buffer = new byte[1024];
            if (NativeMethods.SetupDiGetDeviceRegistryProperty(hDevInfo, ref devInfoData, NativeMethods.SPDRP_HARDWAREID, out _, buffer, (uint)buffer.Length, out _))
            {
                return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
            }
            return string.Empty;
        }

        private string GetDeviceInstanceId(IntPtr hDevInfo, ref NativeMethods.SP_DEVINFO_DATA devInfoData)
        {
            var sb = new StringBuilder(512);
            if (NativeMethods.SetupDiGetDeviceInstanceId(hDevInfo, ref devInfoData, sb, sb.Capacity, out _))
            {
                return sb.ToString();
            }
            return string.Empty;
        }

        public void Dispose()
        {
            _interceptionFilter.Dispose();
        }
    }
}
