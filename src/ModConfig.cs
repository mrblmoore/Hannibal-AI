using System;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace HannibalAI
{
    /// <summary>
    /// Configuration for the HannibalAI mod
    /// </summary>
    public class ModConfig
    {
        private static ModConfig _instance;
        
        public static ModConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ModConfig();
                    _instance.LoadSettings();
                }
                return _instance;
            }
        }
        
        // AI behavior settings
        public bool EnableAI { get; set; } = true;
        public bool VerboseLogging { get; set; } = true;
        public bool ShowHelpMessages { get; set; } = true;
        public float AIUpdateInterval { get; set; } = 3.0f;
        public bool AIControlsEnemies { get; set; } = false;
        public bool UseCommanderMemory { get; set; } = true;
        public bool Debug { get; set; } = false;
        public int Aggressiveness { get; set; } = 50;
        
        // Formation preferences
        public bool PreferHighGround { get; set; } = true;
        public bool PreferRangedFormations { get; set; } = true;
        public bool AggressiveCavalry { get; set; } = true;
        public bool DefensiveInfantry { get; set; } = false;
        
        // Tactical weights (0.0 to 1.0)
        public float FlankingWeight { get; set; } = 0.7f;
        public float DefensiveWeight { get; set; } = 0.5f;
        public float AggressiveWeight { get; set; } = 0.6f;
        
        // File paths
        private const string CONFIG_FILE_NAME = "HannibalAIConfig.xml";
        private string ConfigFilePath => Path.Combine(GetModuleRootPath(), CONFIG_FILE_NAME);
        
        /// <summary>
        /// Load settings from config file
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    using (StreamReader reader = new StreamReader(ConfigFilePath))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(ModConfig));
                        ModConfig loadedConfig = (ModConfig)serializer.Deserialize(reader);
                        
                        // Copy loaded values to this instance
                        this.EnableAI = loadedConfig.EnableAI;
                        this.VerboseLogging = loadedConfig.VerboseLogging;
                        this.ShowHelpMessages = loadedConfig.ShowHelpMessages;
                        this.AIUpdateInterval = loadedConfig.AIUpdateInterval;
                        this.AIControlsEnemies = loadedConfig.AIControlsEnemies;
                        this.UseCommanderMemory = loadedConfig.UseCommanderMemory;
                        this.Debug = loadedConfig.Debug;
                        this.Aggressiveness = loadedConfig.Aggressiveness;
                        this.PreferHighGround = loadedConfig.PreferHighGround;
                        this.PreferRangedFormations = loadedConfig.PreferRangedFormations;
                        this.AggressiveCavalry = loadedConfig.AggressiveCavalry;
                        this.DefensiveInfantry = loadedConfig.DefensiveInfantry;
                        this.FlankingWeight = loadedConfig.FlankingWeight;
                        this.DefensiveWeight = loadedConfig.DefensiveWeight;
                        this.AggressiveWeight = loadedConfig.AggressiveWeight;
                    }
                    
                    if (VerboseLogging)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("HannibalAI config loaded successfully"));
                    }
                }
                else
                {
                    // No config file found, create one with default settings
                    SaveSettings();
                    
                    if (VerboseLogging)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Created new HannibalAI config with default settings"));
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error loading HannibalAI config: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Save settings to config file
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                // Ensure mod directory exists
                string directory = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                using (StreamWriter writer = new StreamWriter(ConfigFilePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ModConfig));
                    serializer.Serialize(writer, this);
                }
                
                if (VerboseLogging)
                {
                    InformationManager.DisplayMessage(new InformationMessage("HannibalAI config saved successfully"));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error saving HannibalAI config: {ex.Message}"));
            }
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
