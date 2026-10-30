using System;
using System.Diagnostics;
using System.IO;

namespace LaptopKeyboardAutoToggle
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static readonly string _logDir;
        private static readonly string _logPath;

        static Logger()
        {
            _logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LaptopKeyboardAutoToggle");
            _logPath = Path.Combine(_logDir, "app.log");

            try
            {
                if (!Directory.Exists(_logDir))
                {
                    Directory.CreateDirectory(_logDir);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create log directory: {ex.Message}");
            }
        }

        public static string LogFilePath => _logPath;

        public static void Info(string message) => Log("INFO", message);
        public static void Warn(string message) => Log("WARN", message);
        public static void Error(string message) => Log("ERROR", message);

        private static void Log(string level, string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            Debug.WriteLine(line);

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logPath, line + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Logging failure: {ex.Message}");
                }
            }
        }

        public static void OpenLogFile()
        {
            try
            {
                if (!File.Exists(_logPath))
                {
                    Info("Created new log file on user request.");
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = _logPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Error($"Failed to open log file: {ex.Message}");
            }
        }
    }
}

