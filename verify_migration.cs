using System;
using System.IO;
using System.Text.Json;

namespace TestMigration
{
    public class AppSettings
    {
        public string? ModelPath { get; set; }
        public string HotkeyModifiers { get; set; } = "0x0003";
        public string HotkeyKey { get; set; } = "0x57";
        public bool SaveDebugRecordings { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
    }

    class Program
    {
        static void Main()
        {
            string oldPath = @"D:\My Documents\GitHub\Wisprflow-ALternative\whisper_olive_tiny_gpu_int8.onnx";
            string newName = "WhisperPete";
            
            Console.WriteLine($"Original: {oldPath}");
            
            if (oldPath.Contains("Wisprflow-ALternative", StringComparison.OrdinalIgnoreCase))
            {
                string newPath = oldPath.Replace("Wisprflow-ALternative", newName, StringComparison.OrdinalIgnoreCase);
                Console.WriteLine($"Migrated: {newPath}");
            }
            else
            {
                Console.WriteLine("No migration needed.");
            }
        }
    }
}
