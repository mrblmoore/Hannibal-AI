using System;
using System.IO;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace HannibalAI.Config
{
    public class ModConfig
    {
        private static ModConfig _instance;
        private static readonly string ConfigPath = Path.Combine(BasePath.Name, "Modules", "HannibalAI", "hannibal_ai_config.json");
        private static readonly string CustomConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            "Mount and Blade II Bannerlord",
            "Configs",
            "HannibalAI",
            "hannibal_ai_config.json"
        );

        public string AIEndpoint { get; set; } = "https://api.example.com/hannibal-ai";
        public string APIKey { get; set; } = "your-api-key-here";
        public string LogLevel { get; set; } = "Info";
        public bool EnableDebugMode { get; set; } = false;
        public int BattleAnalysisInterval { get; set; } = 5;
        public int FormationUpdateInterval { get; set; } = 2;
        public int MaxUnitsPerFormation { get; set; } = 100;
        public int MinimumUnitCountForFlank { get; set; } = 20;
        public float RetreatThreshold { get; set; } = 0.3f;
        public float AggressivenessLevel { get; set; } = 0.7f;
        public float TerrainAnalysisWeight { get; set; } = 0.5f;
        public float WeatherEffectWeight { get; set; } = 0.3f;
        public UnitTypePreferences UnitTypePreferences { get; set; } = new UnitTypePreferences();
        public FormationPreferences FormationPreferences { get; set; } = new FormationPreferences();

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
                    string json = File.ReadAllText(CustomConfigPath);
                    return JsonConvert.DeserializeObject<ModConfig>(json);
                }

                // Fall back to default config
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonConvert.DeserializeObject<ModConfig>(json);
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
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(CustomConfigPath, json);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error saving HannibalAI config: {ex.Message}"));
            }
        }
    }

    public class UnitTypePreferences
    {
        public float Infantry { get; set; } = 1.0f;
        public float Cavalry { get; set; } = 1.2f;
        public float Ranged { get; set; } = 0.8f;
        public float HorseArcher { get; set; } = 1.1f;
    }

    public class FormationPreferences
    {
        public float Line { get; set; } = 1.0f;
        public float Square { get; set; } = 0.8f;
        public float Circle { get; set; } = 0.6f;
        public float Scatter { get; set; } = 0.4f;
        public float Loose { get; set; } = 0.7f;
    }
} 