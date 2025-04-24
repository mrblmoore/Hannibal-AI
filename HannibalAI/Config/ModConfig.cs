using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HannibalAI.Config
{
    public class ModConfig
    {
        private static readonly string ConfigFile = Path.Combine(SubModule.GetConfigPath(), "config.json");
        
        public string AIEndpoint { get; set; }
        public string APIKey { get; set; }
        public string LogLevel { get; set; }
        public bool EnableDebugMode { get; set; }

        public PreferencesConfig Preferences { get; set; }
        public BattleAnalysisConfig BattleAnalysis { get; set; }
        public DebugConfig Debug { get; set; }

        public ModConfig()
        {
            AIEndpoint = "https://api.example.com/v1/analyze";
            APIKey = "";
            LogLevel = "Info";
            EnableDebugMode = false;
            Preferences = new PreferencesConfig();
            BattleAnalysis = new BattleAnalysisConfig();
            Debug = new DebugConfig();
        }

        public static ModConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    var json = File.ReadAllText(ConfigFile);
                    var loadedConfig = JsonConvert.DeserializeObject<ModConfig>(json);
                    return loadedConfig ?? new ModConfig();
                }

                // If config doesn't exist, create it from template
                var newConfig = new ModConfig();
                newConfig.Save();
                return newConfig;
            }
            catch (Exception ex)
            {
                File.AppendAllText("hannibal_ai_errors.log", $"{DateTime.Now}: Error loading config: {ex.Message}\n");
                return new ModConfig();
            }
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(ConfigFile, json);
            }
            catch (Exception ex)
            {
                File.AppendAllText("hannibal_ai_errors.log", $"{DateTime.Now}: Error saving config: {ex.Message}\n");
            }
        }
    }

    public class PreferencesConfig
    {
        public Dictionary<string, string> PreferredFormations { get; set; }
        public Dictionary<string, UnitTypePreference> UnitTypePreferences { get; set; }

        public PreferencesConfig()
        {
            PreferredFormations = new Dictionary<string, string>
            {
                { "Infantry", "Line" },
                { "Ranged", "Loose" },
                { "Cavalry", "Column" },
                { "HorseArcher", "Scatter" }
            };

            UnitTypePreferences = new Dictionary<string, UnitTypePreference>
            {
                { "Infantry", new UnitTypePreference { MinimumUnitCount = 20, PreferredPosition = "Center", AggressivenessLevel = 0.7f } },
                { "Ranged", new UnitTypePreference { MinimumUnitCount = 15, PreferredPosition = "Rear", AggressivenessLevel = 0.3f } },
                { "Cavalry", new UnitTypePreference { MinimumUnitCount = 10, PreferredPosition = "Flanks", AggressivenessLevel = 0.8f } },
                { "HorseArcher", new UnitTypePreference { MinimumUnitCount = 8, PreferredPosition = "Flanks", AggressivenessLevel = 0.5f } }
            };
        }
    }

    public class UnitTypePreference
    {
        public int MinimumUnitCount { get; set; }
        public string PreferredPosition { get; set; }
        public float AggressivenessLevel { get; set; }
    }

    public class BattleAnalysisConfig
    {
        public int UpdateIntervalSeconds { get; set; }
        public bool TerrainAnalysisEnabled { get; set; }
        public bool WeatherAnalysisEnabled { get; set; }
        public bool FormationAnalysisEnabled { get; set; }

        public BattleAnalysisConfig()
        {
            UpdateIntervalSeconds = 5;
            TerrainAnalysisEnabled = true;
            WeatherAnalysisEnabled = true;
            FormationAnalysisEnabled = true;
        }
    }

    public class DebugConfig
    {
        public bool SaveBattleSnapshots { get; set; }
        public bool DetailedLogging { get; set; }
        public bool SaveReplayData { get; set; }

        public DebugConfig()
        {
            SaveBattleSnapshots = false;
            DetailedLogging = false;
            SaveReplayData = false;
        }
    }
}
