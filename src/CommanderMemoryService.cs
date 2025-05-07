using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI
{
    /// <summary>
    /// Service for tracking commander memories between battles
    /// Implements the Nemesis system for recurring enemy commanders
    /// </summary>
    public class CommanderMemoryService
    {
        private static CommanderMemoryService _instance;
        
        public static CommanderMemoryService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CommanderMemoryService();
                    _instance.LoadFromFile();
                }
                return _instance;
            }
        }

        // Commander memory properties
        public int TimesDefeatedPlayer { get; set; }
        public string PreferredFormation { get; set; }
        public float AggressivenessScore { get; set; }
        public bool HasVendettaAgainstPlayer { get; set; }
        public string CommanderNickname { get; set; }
        public int PlayerDefeats { get; set; }
        public int PlayerKills { get; set; }
        public Dictionary<string, int> FormationPreferences { get; set; }
        
        // File path for saving/loading data
        private const string MEMORY_FILE_NAME = "HannibalAICommanderMemory.xml";
        private string MemoryFilePath => Path.Combine(GetModuleRootPath(), MEMORY_FILE_NAME);
        
        private CommanderMemoryService()
        {
            // Initialize with default values
            TimesDefeatedPlayer = 0;
            PreferredFormation = "Line";
            AggressivenessScore = 0.5f;
            HasVendettaAgainstPlayer = false;
            CommanderNickname = "";
            PlayerDefeats = 0;
            PlayerKills = 0;
            FormationPreferences = new Dictionary<string, int>
            {
                { "Line", 0 },
                { "Circle", 0 },
                { "Wedge", 0 },
                { "Column", 0 },
                { "ShieldWall", 0 }
            };
        }
        
        /// <summary>
        /// Record when player defeats a commander
        /// </summary>
        public void RecordPlayerVictory(string enemyFaction)
        {
            PlayerDefeats++;
            
            // Update vendetta status based on repeated losses
            if (PlayerDefeats >= 3)
            {
                HasVendettaAgainstPlayer = true;
                
                // Generate a nickname if we don't have one yet
                if (string.IsNullOrEmpty(CommanderNickname))
                {
                    CommanderNickname = GenerateCommanderNickname();
                }
                
                if (ModConfig.Instance.Debug)
                {
                    Logger.Instance.Info($"HannibalAI: {CommanderNickname} now has a vendetta against you!");
                }
            }
            
            // Adjust aggressiveness based on repeated losses
            AggressivenessScore = Math.Max(0.2f, AggressivenessScore - 0.1f);
            
            SaveToFile();
        }
        
        /// <summary>
        /// Record when a commander defeats the player
        /// </summary>
        public void RecordCommanderVictory()
        {
            TimesDefeatedPlayer++;
            
            // Update commander behavior based on player defeats
            if (TimesDefeatedPlayer >= 3 && string.IsNullOrEmpty(CommanderNickname))
            {
                CommanderNickname = GenerateCommanderNickname();
                
                if (ModConfig.Instance.Debug)
                {
                    Logger.Instance.Info($"HannibalAI: {CommanderNickname} has emerged as your nemesis!");
                }
            }
            
            // Increase aggressiveness with victories
            AggressivenessScore = Math.Min(1.0f, AggressivenessScore + 0.1f);
            
            SaveToFile();
        }
        
        /// <summary>
        /// Record commander's formation preference
        /// </summary>
        public void RecordFormationPreference(string formationType)
        {
            if (FormationPreferences.ContainsKey(formationType))
            {
                FormationPreferences[formationType]++;
                
                // Update preferred formation based on usage
                PreferredFormation = GetMostUsedFormation();
            }
            
            SaveToFile();
        }
        
        /// <summary>
        /// Record when a commander kills player
        /// </summary>
        public void RecordPlayerKill()
        {
            PlayerKills++;
            HasVendettaAgainstPlayer = true;
            
            if (string.IsNullOrEmpty(CommanderNickname))
            {
                CommanderNickname = GenerateCommanderNickname();
            }
            
            // Significant boost to aggressiveness
            AggressivenessScore = Math.Min(1.0f, AggressivenessScore + 0.2f);
            
            SaveToFile();
        }
        
        /// <summary>
        /// Generate a nickname for the commander based on their behavior
        /// </summary>
        private string GenerateCommanderNickname()
        {
            string[] nicknames = {
                "the Stubborn",
                "the Cunning",
                "the Merciless",
                "the Vengeful",
                "the Tactician",
                "the Ruthless",
                "the Relentless",
                "the Unyielding",
                "the Strategist",
                "the Shadow"
            };
            
            // Determine appropriate nickname based on stats
            if (TimesDefeatedPlayer > PlayerDefeats)
            {
                return "the Victorious";
            }
            else if (PlayerKills > 0)
            {
                return "the Slayer";
            }
            else if (AggressivenessScore > 0.7f)
            {
                return "the Fierce";
            }
            else if (AggressivenessScore < 0.3f)
            {
                return "the Cautious";
            }
            
            // Default to random nickname
            Random random = new Random();
            return nicknames[random.Next(nicknames.Length)];
        }
        
        /// <summary>
        /// Get the most used formation type
        /// </summary>
        private string GetMostUsedFormation()
        {
            string mostUsed = "Line"; // Default
            int highestCount = 0;
            
            foreach (var pair in FormationPreferences)
            {
                if (pair.Value > highestCount)
                {
                    highestCount = pair.Value;
                    mostUsed = pair.Key;
                }
            }
            
            return mostUsed;
        }
        
        /// <summary>
        /// Save memory to file
        /// </summary>
        public void SaveToFile(string path = null)
        {
            try
            {
                string filePath = path ?? MemoryFilePath;
                
                // Change extension to xml for clarity
                filePath = Path.ChangeExtension(filePath, ".xml");
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Create a memory data class for serialization
                var memoryData = new CommanderMemoryData
                {
                    TimesDefeatedPlayer = this.TimesDefeatedPlayer,
                    PreferredFormation = this.PreferredFormation,
                    AggressivenessScore = this.AggressivenessScore,
                    HasVendettaAgainstPlayer = this.HasVendettaAgainstPlayer,
                    CommanderNickname = this.CommanderNickname,
                    PlayerDefeats = this.PlayerDefeats,
                    PlayerKills = this.PlayerKills
                    // Note: Dictionary can't be easily serialized with XML, so we'll handle it separately
                };
                
                // Serialize using XML
                var serializer = new XmlSerializer(typeof(CommanderMemoryData));
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(fs, memoryData);
                }
                
                if (ModConfig.Instance.Debug)
                {
                    Logger.Instance.Info("HannibalAI commander memory saved successfully");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error saving HannibalAI commander memory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load memory from file
        /// </summary>
        public void LoadFromFile(string path = null)
        {
            try
            {
                string filePath = path ?? MemoryFilePath;
                
                // Change extension to xml for clarity
                filePath = Path.ChangeExtension(filePath, ".xml");
                
                if (File.Exists(filePath))
                {
                    // Deserialize using XML
                    var serializer = new XmlSerializer(typeof(CommanderMemoryData));
                    CommanderMemoryData memoryData;
                    
                    using (FileStream fs = new FileStream(filePath, FileMode.Open))
                    {
                        memoryData = (CommanderMemoryData)serializer.Deserialize(fs);
                    }
                    
                    // Copy loaded values to this instance
                    this.TimesDefeatedPlayer = memoryData.TimesDefeatedPlayer;
                    this.PreferredFormation = memoryData.PreferredFormation;
                    this.AggressivenessScore = memoryData.AggressivenessScore;
                    this.HasVendettaAgainstPlayer = memoryData.HasVendettaAgainstPlayer;
                    this.CommanderNickname = memoryData.CommanderNickname;
                    this.PlayerDefeats = memoryData.PlayerDefeats;
                    this.PlayerKills = memoryData.PlayerKills;
                    
                    if (ModConfig.Instance.Debug)
                    {
                        Logger.Instance.Info("HannibalAI commander memory loaded successfully");
                    }
                }
                else if (ModConfig.Instance.Debug)
                {
                    Logger.Instance.Info("No previous HannibalAI commander memory found");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error loading HannibalAI commander memory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Simple data class for XML serialization
        /// </summary>
        [Serializable]
        public class CommanderMemoryData
        {
            public int TimesDefeatedPlayer { get; set; }
            public string PreferredFormation { get; set; }
            public float AggressivenessScore { get; set; }
            public bool HasVendettaAgainstPlayer { get; set; }
            public string CommanderNickname { get; set; }
            public int PlayerDefeats { get; set; }
            public int PlayerKills { get; set; }
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