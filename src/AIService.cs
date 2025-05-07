using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace HannibalAI
{
    /// <summary>
    /// Service class that creates and initializes AI components
    /// </summary>
    public class AIService
    {
        private readonly ModConfig _config;
        
        public AIService(ModConfig config)
        {
            _config = config;
        }
        
        /// <summary>
        /// Create a new AI commander instance
        /// </summary>
        public AICommander CreateCommander()
        {
            try
            {
                return new AICommander(_config);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error creating AI commander: {ex.Message}"));
                return null;
            }
        }
        
        /// <summary>
        /// Log informational message
        /// </summary>
        public void LogInfo(string message)
        {
            if (_config.VerboseLogging)
            {
                InformationManager.DisplayMessage(new InformationMessage($"HannibalAI: {message}"));
            }
        }
        
        /// <summary>
        /// Log error message
        /// </summary>
        public void LogError(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage($"HannibalAI Error: {message}"));
        }
    }
}
