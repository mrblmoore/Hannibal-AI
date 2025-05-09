using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI
{
    /// <summary>
    /// XML-based configuration system for the HannibalAI mod
    /// Similar to RBM's approach but simplified
    /// </summary>
    [Serializable]
    [XmlRoot("HannibalAIConfig")]
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
                    
                    // Log the initialization
                    Logger.Instance.Info("HannibalAI ModConfig initialized");
                }
                return _instance;
            }
        }
        
        // AI behavior settings
        [XmlElement("EnableAI")]
        public bool EnableAI { get; set; } = true;
        
        [XmlElement("VerboseLogging")]
        public bool VerboseLogging { get; set; } = true;
        
        [XmlElement("ShowHelpMessages")]
        public bool ShowHelpMessages { get; set; } = true;
        
        [XmlElement("AIUpdateInterval")]
        public float AIUpdateInterval { get; set; } = 3.0f;
        
        [XmlElement("AIControlsEnemies")]
        public bool AIControlsEnemies { get; set; } = false;
        
        [XmlElement("UseCommanderMemory")]
        public bool UseCommanderMemory { get; set; } = true;
        
        [XmlElement("Debug")]
        public bool Debug { get; set; } = false;
        
        [XmlElement("Aggressiveness")]
        public int Aggressiveness { get; set; } = 50;
        
        // Formation preferences
        [XmlElement("PreferHighGround")]
        public bool PreferHighGround { get; set; } = true;
        
        [XmlElement("PreferRangedFormations")]
        public bool PreferRangedFormations { get; set; } = true;
        
        [XmlElement("AggressiveCavalry")]
        public bool AggressiveCavalry { get; set; } = true;
        
        [XmlElement("DefensiveInfantry")]
        public bool DefensiveInfantry { get; set; } = false;
        
        // Tactical weights (0.0 to 1.0)
        [XmlElement("FlankingWeight")]
        public float FlankingWeight { get; set; } = 0.7f;
        
        [XmlElement("DefensiveWeight")]
        public float DefensiveWeight { get; set; } = 0.5f;
        
        [XmlElement("AggressiveWeight")]
        public float AggressiveWeight { get; set; } = 0.6f;
        
        // File paths
        private const string CONFIG_FILE_NAME = "HannibalAIConfig.xml";
        private string ConfigFilePath => Path.Combine(GetModuleRootPath(), CONFIG_FILE_NAME);
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public ModConfig()
        {
            // Initialize with default values
        }
        
        /// <summary>
        /// Load settings from config file
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                Logger.Instance.Info($"Trying to load config from: {ConfigFilePath}");
                
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
                    
                    Logger.Instance.Info("HannibalAI config loaded successfully");
                    
                    if (VerboseLogging)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("HannibalAI config loaded successfully"));
                    }
                    
                    // Log loaded values for debugging
                    LogConfigValues();
                }
                else
                {
                    // No config file found, create one with default settings
                    Logger.Instance.Info($"Config file not found, creating new one at: {ConfigFilePath}");
                    SaveSettings();
                    
                    if (VerboseLogging)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Created new HannibalAI config with default settings"));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error loading HannibalAI config: {ex.Message}\n{ex.StackTrace}");
                InformationManager.DisplayMessage(new InformationMessage($"Error loading HannibalAI config: {ex.Message}", Color.FromUint(0xFF0000)));
            }
        }
        
        /// <summary>
        /// Log the current configuration values
        /// </summary>
        private void LogConfigValues()
        {
            if (!Debug && !VerboseLogging) return;
            
            Logger.Instance.Info("HannibalAI Configuration:");
            Logger.Instance.Info($"- EnableAI: {EnableAI}");
            Logger.Instance.Info($"- VerboseLogging: {VerboseLogging}");
            Logger.Instance.Info($"- ShowHelpMessages: {ShowHelpMessages}");
            Logger.Instance.Info($"- AIControlsEnemies: {AIControlsEnemies}");
            Logger.Instance.Info($"- UseCommanderMemory: {UseCommanderMemory}");
            Logger.Instance.Info($"- Debug: {Debug}");
            Logger.Instance.Info($"- Aggressiveness: {Aggressiveness}");
            Logger.Instance.Info($"- AIUpdateInterval: {AIUpdateInterval}");
            
            // Log tactical weights if in debug mode
            if (Debug)
            {
                Logger.Instance.Info("Tactical Weights:");
                Logger.Instance.Info($"- FlankingWeight: {FlankingWeight}");
                Logger.Instance.Info($"- DefensiveWeight: {DefensiveWeight}");
                Logger.Instance.Info($"- AggressiveWeight: {AggressiveWeight}");
                Logger.Instance.Info($"- PreferHighGround: {PreferHighGround}");
                Logger.Instance.Info($"- PreferRangedFormations: {PreferRangedFormations}");
                Logger.Instance.Info($"- AggressiveCavalry: {AggressiveCavalry}");
                Logger.Instance.Info($"- DefensiveInfantry: {DefensiveInfantry}");
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
                
                // Create XML file with proper formatting
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    Indent = true,
                    IndentChars = "    ",
                    NewLineOnAttributes = false,
                    OmitXmlDeclaration = false
                };
                
                using (XmlWriter writer = XmlWriter.Create(ConfigFilePath, settings))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ModConfig));
                    serializer.Serialize(writer, this);
                }
                
                Logger.Instance.Info($"HannibalAI config saved to: {ConfigFilePath}");
                
                if (VerboseLogging)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "HannibalAI config saved successfully", 
                        Color.FromUint(0x00FF00)));
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error saving HannibalAI config: {ex.Message}\n{ex.StackTrace}");
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Error saving HannibalAI config: {ex.Message}", 
                    Color.FromUint(0xFF0000)));
            }
        }
        
        /// <summary>
        /// Get the root path of the mod
        /// </summary>
        private string GetModuleRootPath()
        {
            try
            {
                // Try to use Bannerlord's module system if available
                string moduleRoot = AppDomain.CurrentDomain.BaseDirectory;
                string modPath = Path.Combine(moduleRoot, "Modules", "HannibalAI");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(modPath))
                {
                    Directory.CreateDirectory(modPath);
                    Logger.Instance.Info($"Created mod directory: {modPath}");
                }
                
                return modPath;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error determining module path: {ex.Message}");
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }
    }
}
