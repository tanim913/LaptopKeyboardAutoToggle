using System;
using System.IO;
using System.Text.Json;

namespace LaptopKeyboardAutoToggle
{
    public class AppConfig
    {
        public string InternalKeyboardHardwareId { get; set; } = "ACPI\\HPQ8002";
        public string UsbDongleHardwareId { get; set; } = "VID_1EA7&PID_0066";
        public string EmergencyHotkey { get; set; } = "Ctrl+Alt+Shift+F12";
        public bool DisableParentIfChildFails { get; set; } = true;
        public string LogFileName { get; set; } = "app.log";

        public static AppConfig Load()
        {
            string configPath = GetConfigFilePath();
            if (!File.Exists(configPath))
            {
                var defaultConfig = new AppConfig();
                defaultConfig.Save();
                return defaultConfig;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<AppConfig>(json, options);
                return config ?? new AppConfig();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to parse config.json at {configPath}, using defaults: {ex.Message}");
                return new AppConfig();
            }
        }

        public void Save()
        {
            try
            {
                string configPath = GetConfigFilePath();
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save config.json: {ex.Message}");
            }
        }

        public static string GetConfigFilePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "config.json");
        }
    }
}

