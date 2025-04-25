using System;
using System.IO;
using TaleWorlds.Library;

namespace HannibalAI.Utils
{
    public static class Logger
    {
        private static readonly string LogFile = "HannibalAI.log";

        public static void Log(string message, bool isError = false)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logMessage = $"[{timestamp}] {(isError ? "ERROR" : "INFO")}: {message}";
                
                File.AppendAllText(LogFile, logMessage + Environment.NewLine);
                
                if (isError)
                {
                    Debug.Print($"[HannibalAI] ERROR: {message}");
                }
                else
                {
                    Debug.Print($"[HannibalAI] INFO: {message}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Failed to log message: {ex.Message}");
            }
        }

        public static void LogError(string message)
        {
            Log(message, true);
        }

        public static void LogInfo(string message)
        {
            Log(message, false);
        }
    }
} 