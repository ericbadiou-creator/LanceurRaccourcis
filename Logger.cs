using System;
using System.IO;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace LanceurRaccourcis
{
    // Classe statique pour le logging
    [SupportedOSPlatform("windows")]
    public static class Logger
    {
        private static readonly string logFilePath;
        private static string logDirectory;
        private static int retentionDays;
        private static readonly object lockObj = new object();

        static Logger()
        {
            string exePath = Application.ExecutablePath;
            string exeDir = Path.GetDirectoryName(exePath) ?? "";
            logFilePath = Path.Combine(exeDir, "LanceurRaccourcis.log");
            logDirectory = exeDir;
            retentionDays = 7; // défaut
            TryEnsureDirectory(logDirectory);
            PurgeOldLogs();
        }

        public static void Configure(string? directory, int? retention)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    logDirectory = directory!;
                    TryEnsureDirectory(logDirectory);
                }
                if (retention.HasValue && retention.Value > 0)
                {
                    retentionDays = retention.Value;
                }
                PurgeOldLogs();
            }
            catch
            {
            }
        }

        private static string GetTodayLogPath()
        {
            string fileName = $"LanceurRaccourcis_{DateTime.Now:yyyy-MM-dd}.log";
            return Path.Combine(logDirectory, fileName);
        }

        private static void TryEnsureDirectory(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch { }
        }

        private static void PurgeOldLogs()
        {
            try
            {
                if (!Directory.Exists(logDirectory)) return;
                var files = Directory.GetFiles(logDirectory, "LanceurRaccourcis_*.log");
                DateTime threshold = DateTime.Now.Date.AddDays(-retentionDays);
                foreach (var f in files)
                {
                    try
                    {
                        var info = new FileInfo(f);
                        if (info.LastWriteTime.Date < threshold)
                        {
                            info.Delete();
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void Log(string message)
        {
            try
            {
                lock (lockObj)
                {
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                    File.AppendAllText(GetTodayLogPath(), logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Si on ne peut pas écrire le log, on ignore silencieusement
            }
        }

        public static void LogError(string message, Exception? ex = null)
        {
            string fullMessage = ex != null ? $"ERREUR: {message} - {ex.Message}" : $"ERREUR: {message}";
            Log(fullMessage);
            if (ex != null && ex.StackTrace != null)
            {
                Log($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}