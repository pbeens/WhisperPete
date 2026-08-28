using System;
using System.IO;
using System.Text.Json;

namespace WhisperPete.Core
{
    public class AppSettings
    {
        public string? ModelPath { get; set; }
        public string HotkeyModifiers { get; set; } = "0x0003"; // Ctrl + Alt
        public string HotkeyKey { get; set; } = "0x57"; // W
        public bool SaveDebugRecordings { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
    }

    public static class SettingsManager
    {
        private static readonly string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WhisperPete");
        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

        private static void EnsureFolderExists()
        {
            try
            {
                if (!Directory.Exists(SettingsFolder)) Directory.CreateDirectory(SettingsFolder);
            }
            catch { }
        }

        public static AppSettings Load()
        {
            try
            {
                // Migration: Check for old settings file in BaseDirectory
                string oldFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                if (File.Exists(oldFile) && !File.Exists(SettingsFile))
                {
                    try 
                    {
                        EnsureFolderExists();
                        File.Move(oldFile, SettingsFile);
                    }
                    catch { }
                }

                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

                    // Migration: Update ModelPath if it contains the old project name
                    if (!string.IsNullOrEmpty(settings.ModelPath) && settings.ModelPath.Contains("Wisprflow-ALternative", StringComparison.OrdinalIgnoreCase))
                    {
                        string oldPath = settings.ModelPath;
                        string newPath = oldPath.Replace("Wisprflow-ALternative", "WhisperPete", StringComparison.OrdinalIgnoreCase);
                        
                        // Only update if the new path actually exists, or if the old one definitely doesn't
                        if (File.Exists(newPath) || !File.Exists(oldPath))
                        {
                            settings.ModelPath = newPath;
                            // Pre-emptively save the migrated path
                            Save(settings);
                        }
                    }

                    return settings;
                }
            }
            catch { }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                EnsureFolderExists();
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch { }
        }
    }
}
