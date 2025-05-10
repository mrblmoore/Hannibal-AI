using System;
using System.IO;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace HannibalAI
{
    /// <summary>
    /// Logging utility for HannibalAI
    /// </summary>
    public class Logger
    {
        private static Logger _instance;
        private readonly ModConfig _config;
        private readonly string _logFilePath;
        
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Logger(ModConfig.Instance);
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Initialize the logger with a specific log file path
        /// </summary>
        public static void Initialize(string logPath)
        {
            if (_instance == null)
            {
                ModConfig config = ModConfig.Instance;
                _instance = new Logger(config, logPath);
            }
        }
        
        private Logger(ModConfig config)
            : this(config, "HannibalAI.log")
        {
        }
        
        private Logger(ModConfig config, string logPath)
        {
            _config = config;
            _logFilePath = logPath;
            
            // Create or clear the log file
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Create a new log file (overwrite existing)
                using (StreamWriter writer = new StreamWriter(_logFilePath, false))
                {
                    writer.WriteLine($"=== HannibalAI Log Started {DateTime.Now} ===");
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Failed to initialize log file: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Log info message
        /// </summary>
        public void Info(string message)
        {
            LogMessage("INFO", message);
            
            if (_config.Debug || _config.VerboseLogging)
            {
                InformationManager.DisplayMessage(new InformationMessage($"HannibalAI: {message}"));
            }
        }
        
        /// <summary>
        /// Log warning message
        /// </summary>
        public void Warning(string message)
        {
            LogMessage("WARNING", message);
            
            if (_config.Debug)
            {
                InformationManager.DisplayMessage(new InformationMessage($"HannibalAI Warning: {message}"));
            }
        }
        
        /// <summary>
        /// Log error message
        /// </summary>
        public void Error(string message)
        {
            LogMessage("ERROR", message);
            InformationManager.DisplayMessage(new InformationMessage($"HannibalAI Error: {message}"));
        }
        
        /// <summary>
        /// Log debug message (only if debug mode is enabled)
        /// </summary>
        public void Debug(string message)
        {
            if (_config.Debug)
            {
                LogMessage("DEBUG", message);
                
                // Automatically add [HannibalAI] prefix for consistency if not already present
                if (!message.Contains("[HannibalAI]"))
                {
                    message = $"[HannibalAI] {message}";
                }
                
                // Also output to system diagnostics for easier tracing in logs
                System.Diagnostics.Debug.Print(message);
                
                // Show in game but with less visual noise than errors/warnings
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Debug: {message}", Color.FromUint(0xAADDFFU)));
            }
        }
        
        /// <summary>
        /// Log detailed diagnostic info with multiple values (only in debug mode)
        /// </summary>
        public void DiagnosticInfo(string context, Dictionary<string, object> values)
        {
            if (!_config.Debug)
                return;
                
            // Create header
            LogMessage("DIAGNOSTIC", $"=== {context} Diagnostic Info ===");
            System.Diagnostics.Debug.Print($"[HannibalAI] === {context} Diagnostic Info ===");
            
            // Log each value
            foreach (var pair in values)
            {
                string value = pair.Value?.ToString() ?? "null";
                LogMessage("DIAGNOSTIC", $"{pair.Key}: {value}");
                System.Diagnostics.Debug.Print($"[HannibalAI] {pair.Key}: {value}");
            }
            
            // Close diagnostic block
            LogMessage("DIAGNOSTIC", $"=== End {context} Diagnostic Info ===");
            System.Diagnostics.Debug.Print($"[HannibalAI] === End {context} Diagnostic Info ===");
            
            // Show short summary in game
            if (_config.VerboseLogging)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Diagnostic info logged for {context} ({values.Count} values)",
                    Color.FromUint(0x88CCEEU)));
            }
        }
        
        /// <summary>
        /// Write message to log file
        /// </summary>
        private void LogMessage(string level, string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_logFilePath, true))
                {
                    string threadId = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
                    string category = GetCategory();
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [Thread:{threadId}] [{category}] {message}");
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Logging failed: {ex.Message}", Color.FromUint(0xFF0000)));
            }
        }

        private string GetCategory()
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            var frames = stackTrace.GetFrames();
            if (frames != null && frames.Length >= 4)
            {
                var method = frames[3].GetMethod();
                return method.DeclaringType?.Name ?? "Unknown";
            }
            return "Unknown";
        }
        
        /// <summary>
        /// Get the root path of the mod
        /// </summary>
        private string GetModuleRootPath()
        {
            try
            {
                // In a Bannerlord environment, would use ModuleHelper
                // For now, use current directory as fallback
                string moduleRoot = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(moduleRoot, "Modules", "HannibalAI");
            }
            catch
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }
    }
}