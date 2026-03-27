using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DisplaySwitcher
{
    public class DisplayProfile
    {
        public string Name { get; set; } = string.Empty;
        public List<ProfileMonitor> Monitors { get; set; } = new();
        public string? AudioDeviceId { get; set; }
        public string? AudioDeviceName { get; set; }
        public string? AudioInputDeviceId { get; set; }
        public string? AudioInputDeviceName { get; set; }
    }

    public class ProfileMonitor
    {
        public string FriendlyName { get; set; } = string.Empty;
        public string DevicePath { get; set; } = string.Empty;
        public string GdiDeviceName { get; set; } = string.Empty;

        // Serialized as strings to avoid LUID struct issues in JSON
        public uint AdapterIdLow { get; set; }
        public int AdapterIdHigh { get; set; }
        public uint TargetId { get; set; }
        public uint SourceId { get; set; }

        // Source mode – resolution & desktop arrangement
        public uint Width { get; set; }
        public uint Height { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public bool IsPrimary { get; set; }
        public uint PixelFormat { get; set; }

        // Target mode – refresh rate & signal info
        public ulong PixelRate { get; set; }
        public uint HSyncFreqN { get; set; }
        public uint HSyncFreqD { get; set; }
        public uint VSyncFreqN { get; set; }
        public uint VSyncFreqD { get; set; }
        public uint ActiveWidth { get; set; }
        public uint ActiveHeight { get; set; }
        public uint TotalWidth { get; set; }
        public uint TotalHeight { get; set; }
        public uint VideoStandard { get; set; }
        public uint ScanLineOrdering { get; set; }

        [JsonIgnore]
        public DisplayConfig.LUID AdapterId => new DisplayConfig.LUID
        {
            LowPart = AdapterIdLow,
            HighPart = AdapterIdHigh
        };
    }

    public class AppSettings
    {
        public List<DisplayProfile> Profiles { get; set; } = new();
        public int ActiveProfileIndex { get; set; } = -1;
        public bool StartWithWindows { get; set; } = false;
    }

    public static class SettingsManager
    {
        private static readonly string _settingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DisplaySwitcher");
        private static readonly string _settingsPath;

        static SettingsManager()
        {
            _settingsPath = Path.Combine(_settingsDir, "settings.json");
            Directory.CreateDirectory(_settingsDir);
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { /* corrupt file → fresh start */ }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, opts));
        }

        public static string SettingsDirectory => _settingsDir;
    }
}
