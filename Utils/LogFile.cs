using System;
using System.IO;

namespace HannibalAI.Utils
{
    public static class LogFile
    {
        private static readonly string _logPath = Path.Combine("Modules", "HannibalAI", "log.txt");

        public static void WriteLine(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(_logPath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Silently fail if we can't write to the log file
                // This prevents any potential issues from logging failures
            }
        }
    }
} 