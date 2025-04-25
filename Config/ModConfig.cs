using System;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.Library;

namespace HannibalAI.Config
{
    public class ModConfig
    {
        private static ModConfig _instance;
        private static readonly string ConfigPath = Path.Combine(BasePath.Name, "Modules", "HannibalAI", "DefaultConfig.xml");
        private static readonly string CustomConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            "Mount and Blade II Bannerlord",
            "Configs",
            "HannibalAI",
            "config.xml"
        );

        public bool Enabled { get; set; } = true;
        public string AIEndpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
        public string APIKey { get; set; } = "";
        public AIServiceConfig AIService { get; set; } = new AIServiceConfig();
        public BattleAnalysisConfig BattleAnalysis { get; set; } = new BattleAnalysisConfig();
        public CommanderLearningConfig CommanderLearning { get; set; } = new CommanderLearningConfig();
        public DebugConfig Debug { get; set; } = new DebugConfig();

        public static ModConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        public static ModConfig Load()
        {
            try
            {
                // Try to load custom config first
                if (File.Exists(CustomConfigPath))
                {
                    using (var reader = new StreamReader(CustomConfigPath))
                    {
                        var serializer = new XmlSerializer(typeof(ModConfig));
                        return (ModConfig)serializer.Deserialize(reader);
                    }
                }

                // Fall back to default config
                if (File.Exists(ConfigPath))
                {
                    using (var reader = new StreamReader(ConfigPath))
                    {
                        var serializer = new XmlSerializer(typeof(ModConfig));
                        return (ModConfig)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error loading HannibalAI config: {ex.Message}"));
            }

            return new ModConfig();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CustomConfigPath));
                using (var writer = new StreamWriter(CustomConfigPath))
                {
                    var serializer = new XmlSerializer(typeof(ModConfig));
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error saving HannibalAI config: {ex.Message}"));
            }
        }
    }

    public class AIServiceConfig
    {
        public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
        public string APIKey { get; set; } = "";
        public string ModelVersion { get; set; } = "gpt-4";
        public int MaxTokens { get; set; } = 1000;
        public float Temperature { get; set; } = 0.7f;
    }

    public class BattleAnalysisConfig
    {
        public float UpdateIntervalSeconds { get; set; } = 1.0f;
        public int MinimumUnitCountForAnalysis { get; set; } = 5;
    }

    public class CommanderLearningConfig
    {
        public bool Enabled { get; set; } = true;
        public float LearningRate { get; set; } = 0.1f;
        public int MemoryPersistenceDays { get; set; } = 30;
        public int MaxStoredBattles { get; set; } = 100;
    }

    public class DebugConfig
    {
        public bool Enabled { get; set; } = false;
        public string LogLevel { get; set; } = "Info";
        public bool ShowAIDecisions { get; set; } = true;
        public bool ShowBattleAnalysis { get; set; } = true;
    }
} 