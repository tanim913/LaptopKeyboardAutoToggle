using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LaptopKeyboardAutoToggle
{
    public class InterceptionFilter : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct KeyStroke
        {
            public ushort Code;
            public ushort State;
            public uint Information;
        }

        public const ushort INTERCEPTION_FILTER_KEY_ALL = 0xFFFF;
        public const ushort INTERCEPTION_FILTER_KEY_NONE = 0x0000;

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern IntPtr interception_create_context();

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void interception_destroy_context(IntPtr context);

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void interception_set_filter(IntPtr context, InterceptionPredicate predicate, ushort filter);

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int interception_receive(IntPtr context, int device, ref KeyStroke stroke, uint nstroke);

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int interception_send(IntPtr context, int device, ref KeyStroke stroke, uint nstroke);

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint interception_get_hardware_id(IntPtr context, int device, byte[] hardwareIdBuffer, uint bufferSize);

        [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int interception_is_keyboard(int device);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int InterceptionPredicate(int device);

        private IntPtr _context = IntPtr.Zero;
        private Thread? _workerThread;
        private volatile bool _isRunning = false;
        private InterceptionPredicate? _predicate;

        public bool IsDriverAvailable { get; private set; } = false;
        public bool SuppressInternalKeyboard { get; set; } = false;

        private readonly string _targetInternalHwId;

        public InterceptionFilter(string targetInternalHwId)
        {
            _targetInternalHwId = targetInternalHwId;
        }

        public bool TryInitialize()
        {
            try
            {
                // Verify if interception.dll exists
                string dllPath = Path.Combine(AppContext.BaseDirectory, "interception.dll");
                if (!File.Exists(dllPath))
                {
                    Logger.Info("interception.dll not found in app directory. Interception mode disabled.");
                    return false;
                }

                _context = interception_create_context();
                if (_context == IntPtr.Zero)
                {
                    Logger.Warn("interception_create_context returned null. Interception driver may not be installed or reboot is pending.");
                    return false;
                }

                _predicate = device => interception_is_keyboard(device);
                interception_set_filter(_context, _predicate, INTERCEPTION_FILTER_KEY_ALL);

                IsDriverAvailable = true;
                _isRunning = true;
                _workerThread = new Thread(FilterLoop)
                {
                    IsBackground = true,
                    Name = "InterceptionWorkerThread"
                };
                _workerThread.Start();

                Logger.Info("Interception kernel keyboard filter successfully initialized and running!");
                return true;
            }
            catch (DllNotFoundException)
            {
                Logger.Info("interception.dll is not loaded.");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Interception initialization exception: {ex.Message}");
                return false;
            }
        }

        private void FilterLoop()
        {
            byte[] hwIdBuffer = new byte[512];
            var stroke = new KeyStroke();

            while (_isRunning && _context != IntPtr.Zero)
            {
                try
                {
                    // Device handles are 1-based (keyboard 1 is typically internal PS/2)
                    for (int dev = 1; dev <= 20; dev++)
                    {
                        if (!_isRunning) break;

                        if (interception_is_keyboard(dev) != 0)
                        {
                            if (interception_receive(_context, dev, ref stroke, 1) > 0)
                            {
                                string hwId = "";
                                uint bytesRead = interception_get_hardware_id(_context, dev, hwIdBuffer, (uint)hwIdBuffer.Length);
                                if (bytesRead > 0)
                                {
                                    hwId = Encoding.Unicode.GetString(hwIdBuffer, 0, (int)bytesRead).TrimEnd('\0');
                                }

                                bool isInternal = false;
                                if (!string.IsNullOrEmpty(hwId) && hwId.Contains(_targetInternalHwId, StringComparison.OrdinalIgnoreCase))
                                {
                                    isInternal = true;
                                }
                                else if (hwId.StartsWith("ACPI", StringComparison.OrdinalIgnoreCase) || hwId.Contains("HPQ", StringComparison.OrdinalIgnoreCase))
                                {
                                    isInternal = true;
                                }

                                // If suppression is enabled and this is the internal keyboard, drop the key!
                                if (SuppressInternalKeyboard && isInternal)
                                {
                                    // Keystroke discarded (suppressed)
                                    continue;
                                }

                                // Otherwise pass through immediately
                                interception_send(_context, dev, ref stroke, 1);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in Interception loop: {ex.Message}");
                    Thread.Sleep(50);
                }
            }
        }

        public void Dispose()
        {
            _isRunning = false;
            if (_context != IntPtr.Zero)
            {
                try
                {
                    interception_destroy_context(_context);
                }
                catch { }
                _context = IntPtr.Zero;
            }
        }
    }
}

