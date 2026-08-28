using System;
using System.IO;

namespace WhisperPete.Core
{
    public static class Logger
    {
        private static readonly string LogFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WhisperPete");
        private static readonly string LogPath = Path.Combine(LogFolder, "app_log.txt");

        private static void EnsureFolderExists()
        {
            try
            {
                if (!Directory.Exists(LogFolder)) Directory.CreateDirectory(LogFolder);
            }
            catch { }
        }

        public static void Log(string message)
        {
            try
            {
                EnsureFolderExists();
                File.AppendAllText(LogPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
                Console.WriteLine(message);
            }
            catch { }
        }
    }
}
