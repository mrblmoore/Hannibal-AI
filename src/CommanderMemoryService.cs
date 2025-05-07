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
        
        // Additional tactical properties
        public Dictionary<string, int> TerrainPreferences { get; set; }
        public Dictionary<string, int> TacticSuccessRates { get; set; }
        public List<string> LearnedWeaknesses { get; set; }
        public List<string> LearnedStrengths { get; set; }
        public int BattlesCount { get; set; }
        public bool IsAdaptiveCommanderEnabled { get; set; } 
        public DateTime LastBattleTimestamp { get; set; }
        public string LastBattleResult { get; set; }
        public Dictionary<string, float> UnitTypeEffectiveness { get; set; }
        
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
            BattlesCount = 0;
            IsAdaptiveCommanderEnabled = true;
            LastBattleTimestamp = DateTime.Now;
            LastBattleResult = "None";
            
            // Initialize formations preferences
            FormationPreferences = new Dictionary<string, int>
            {
                { "Line", 0 },
                { "Circle", 0 },
                { "Wedge", 0 },
                { "Column", 0 },
                { "ShieldWall", 0 }
            };
            
            // Initialize terrain preferences
            TerrainPreferences = new Dictionary<string, int>
            {
                { "HighGround", 0 },
                { "Forest", 0 },
                { "Open", 0 },
                { "River", 0 },
                { "Hills", 0 }
            };
            
            // Initialize tactic success tracking
            TacticSuccessRates = new Dictionary<string, int>
            {
                { "Defensive", 0 },
                { "Offensive", 0 },
                { "Flanking", 0 },
                { "RangedFocus", 0 },
                { "CavalryCharge", 0 }
            };
            
            // Initialize unit effectiveness tracking
            UnitTypeEffectiveness = new Dictionary<string, float>
            {
                { "Infantry", 1.0f },
                { "Ranged", 1.0f },
                { "Cavalry", 1.0f },
                { "HorseArcher", 1.0f }
            };
            
            LearnedWeaknesses = new List<string>();
            LearnedStrengths = new List<string>();
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
        /// Record a tactical decision and its outcome for learning
        /// </summary>
        public void RecordTacticOutcome(string tacticName, bool wasSuccessful, float effectivenessScore)
        {
            // Increment battles count
            BattlesCount++;
            
            // Update last battle time
            LastBattleTimestamp = DateTime.Now;
            
            // Record success/failure of tactic
            if (TacticSuccessRates.ContainsKey(tacticName))
            {
                if (wasSuccessful)
                {
                    TacticSuccessRates[tacticName]++;
                }
            }
            else
            {
                TacticSuccessRates[tacticName] = wasSuccessful ? 1 : 0;
            }
            
            // Update last battle result
            LastBattleResult = wasSuccessful ? "Victory" : "Defeat";
            
            SaveToFile();
        }
        
        /// <summary>
        /// Record that a specific terrain type was advantageous
        /// </summary>
        public void RecordTerrainPreference(string terrainType, bool wasAdvantageous)
        {
            if (TerrainPreferences.ContainsKey(terrainType))
            {
                if (wasAdvantageous)
                {
                    TerrainPreferences[terrainType]++;
                }
            }
            else
            {
                TerrainPreferences[terrainType] = wasAdvantageous ? 1 : 0;
            }
            
            SaveToFile();
        }
        
        /// <summary>
        /// Record effectiveness of a unit type in battle
        /// </summary>
        public void RecordUnitTypeEffectiveness(string unitType, float effectivenessScore)
        {
            if (UnitTypeEffectiveness.ContainsKey(unitType))
            {
                // Apply rolling average to update effectiveness score
                UnitTypeEffectiveness[unitType] = (UnitTypeEffectiveness[unitType] * 0.7f) + (effectivenessScore * 0.3f);
            }
            else
            {
                UnitTypeEffectiveness[unitType] = effectivenessScore;
            }
            
            SaveToFile();
        }
        
        /// <summary>
        /// Add a learned weakness of the player
        /// </summary>
        public void AddLearnedWeakness(string weakness)
        {
            if (!LearnedWeaknesses.Contains(weakness))
            {
                LearnedWeaknesses.Add(weakness);
                SaveToFile();
            }
        }
        
        /// <summary>
        /// Add a learned strength of the player
        /// </summary>
        public void AddLearnedStrength(string strength)
        {
            if (!LearnedStrengths.Contains(strength))
            {
                LearnedStrengths.Add(strength);
                SaveToFile();
            }
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
        /// Get tactical advice based on commander memory to be used by the AI
        /// </summary>
        public TacticalAdvice GetTacticalAdvice()
        {
            TacticalAdvice advice = new TacticalAdvice();
            
            // Default values
            advice.SuggestedAggression = AggressivenessScore;
            advice.PreferredFormationType = PreferredFormation;
            advice.HasLearningData = BattlesCount > 0;
            
            // Find most successful tactics
            string bestTactic = "Balanced";
            int highestSuccess = 0;
            
            foreach (var pair in TacticSuccessRates)
            {
                if (pair.Value > highestSuccess)
                {
                    highestSuccess = pair.Value;
                    bestTactic = pair.Key;
                }
            }
            
            advice.RecommendedTactic = bestTactic;
            
            // Find best terrain preference
            string bestTerrain = "Open";
            int highestTerrainCount = 0;
            
            foreach (var pair in TerrainPreferences)
            {
                if (pair.Value > highestTerrainCount)
                {
                    highestTerrainCount = pair.Value;
                    bestTerrain = pair.Key;
                }
            }
            
            advice.PreferredTerrain = bestTerrain;
            
            // Add all learned weaknesses and strengths
            advice.PlayerWeaknesses = new List<string>(LearnedWeaknesses);
            advice.PlayerStrengths = new List<string>(LearnedStrengths);
            
            // Find best unit types to use
            advice.UnitEffectiveness = new Dictionary<string, float>(UnitTypeEffectiveness);
            
            // Extra adaptive behaviors for nemesis commanders
            if (HasVendettaAgainstPlayer)
            {
                advice.HasVendettaAgainstPlayer = true;
                advice.CommanderTitle = CommanderNickname;
                
                // Nemesis commanders become more extreme in their tactics
                if (AggressivenessScore > 0.6f)
                {
                    advice.SuggestedAggression = Math.Min(1.0f, AggressivenessScore + 0.1f);
                }
                else if (AggressivenessScore < 0.4f)
                {
                    advice.SuggestedAggression = Math.Max(0.0f, AggressivenessScore - 0.1f);
                }
            }
            
            return advice;
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
                
                // Convert dictionaries and lists to string representation for serialization
                string terrainPreferencesData = SerializeDictionary(TerrainPreferences);
                string tacticSuccessRatesData = SerializeDictionary(TacticSuccessRates);
                string unitTypeEffectivenessData = SerializeDictionary(UnitTypeEffectiveness);
                string formationPreferencesData = SerializeDictionary(FormationPreferences);
                string learnedWeaknessesData = SerializeList(LearnedWeaknesses);
                string learnedStrengthsData = SerializeList(LearnedStrengths);
                
                // Create a memory data class for serialization
                var memoryData = new CommanderMemoryData
                {
                    // Basic properties
                    TimesDefeatedPlayer = this.TimesDefeatedPlayer,
                    PreferredFormation = this.PreferredFormation,
                    AggressivenessScore = this.AggressivenessScore,
                    HasVendettaAgainstPlayer = this.HasVendettaAgainstPlayer,
                    CommanderNickname = this.CommanderNickname,
                    PlayerDefeats = this.PlayerDefeats,
                    PlayerKills = this.PlayerKills,
                    
                    // Enhanced properties
                    BattlesCount = this.BattlesCount,
                    IsAdaptiveCommanderEnabled = this.IsAdaptiveCommanderEnabled,
                    LastBattleTimestamp = this.LastBattleTimestamp,
                    LastBattleResult = this.LastBattleResult,
                    
                    // Serialized collections
                    TerrainPreferencesData = terrainPreferencesData,
                    TacticSuccessRatesData = tacticSuccessRatesData,
                    UnitTypeEffectivenessData = unitTypeEffectivenessData,
                    FormationPreferencesData = formationPreferencesData,
                    LearnedWeaknessesData = learnedWeaknessesData,
                    LearnedStrengthsData = learnedStrengthsData
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
                    
                    // Copy basic loaded values to this instance
                    this.TimesDefeatedPlayer = memoryData.TimesDefeatedPlayer;
                    this.PreferredFormation = memoryData.PreferredFormation;
                    this.AggressivenessScore = memoryData.AggressivenessScore;
                    this.HasVendettaAgainstPlayer = memoryData.HasVendettaAgainstPlayer;
                    this.CommanderNickname = memoryData.CommanderNickname;
                    this.PlayerDefeats = memoryData.PlayerDefeats;
                    this.PlayerKills = memoryData.PlayerKills;
                    
                    // Copy enhanced properties if available
                    this.BattlesCount = memoryData.BattlesCount;
                    this.IsAdaptiveCommanderEnabled = memoryData.IsAdaptiveCommanderEnabled;
                    this.LastBattleTimestamp = memoryData.LastBattleTimestamp;
                    this.LastBattleResult = memoryData.LastBattleResult;
                    
                    // Deserialize collections if they exist
                    if (!string.IsNullOrEmpty(memoryData.TerrainPreferencesData))
                    {
                        this.TerrainPreferences = DeserializeDictionaryInt(memoryData.TerrainPreferencesData);
                    }
                    
                    if (!string.IsNullOrEmpty(memoryData.TacticSuccessRatesData))
                    {
                        this.TacticSuccessRates = DeserializeDictionaryInt(memoryData.TacticSuccessRatesData);
                    }
                    
                    if (!string.IsNullOrEmpty(memoryData.FormationPreferencesData))
                    {
                        this.FormationPreferences = DeserializeDictionaryInt(memoryData.FormationPreferencesData);
                    }
                    
                    if (!string.IsNullOrEmpty(memoryData.UnitTypeEffectivenessData))
                    {
                        this.UnitTypeEffectiveness = DeserializeDictionaryFloat(memoryData.UnitTypeEffectivenessData);
                    }
                    
                    if (!string.IsNullOrEmpty(memoryData.LearnedWeaknessesData))
                    {
                        this.LearnedWeaknesses = DeserializeList(memoryData.LearnedWeaknessesData);
                    }
                    
                    if (!string.IsNullOrEmpty(memoryData.LearnedStrengthsData))
                    {
                        this.LearnedStrengths = DeserializeList(memoryData.LearnedStrengthsData);
                    }
                    
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
            // Basic properties
            public int TimesDefeatedPlayer { get; set; }
            public string PreferredFormation { get; set; }
            public float AggressivenessScore { get; set; }
            public bool HasVendettaAgainstPlayer { get; set; }
            public string CommanderNickname { get; set; }
            public int PlayerDefeats { get; set; }
            public int PlayerKills { get; set; }
            
            // Enhanced properties
            public int BattlesCount { get; set; }
            public bool IsAdaptiveCommanderEnabled { get; set; }
            public DateTime LastBattleTimestamp { get; set; }
            public string LastBattleResult { get; set; }
            
            // For dictionaries and lists, we'll use string representations 
            // These will be parsed by the CommanderMemoryService
            public string TerrainPreferencesData { get; set; }
            public string TacticSuccessRatesData { get; set; }
            public string LearnedWeaknessesData { get; set; }
            public string LearnedStrengthsData { get; set; }
            public string UnitTypeEffectivenessData { get; set; }
            public string FormationPreferencesData { get; set; }
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
        
        /// <summary>
        /// Helper method to serialize a dictionary with int values to a string
        /// </summary>
        private string SerializeDictionary<TValue>(Dictionary<string, TValue> dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
            {
                return string.Empty;
            }
            
            List<string> entries = new List<string>();
            foreach (var pair in dictionary)
            {
                entries.Add($"{pair.Key}:{pair.Value}");
            }
            
            return string.Join("|", entries);
        }
        
        /// <summary>
        /// Helper method to deserialize a dictionary with int values from a string
        /// </summary>
        private Dictionary<string, int> DeserializeDictionaryInt(string serialized)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            
            if (string.IsNullOrEmpty(serialized))
            {
                return result;
            }
            
            string[] entries = serialized.Split('|');
            foreach (var entry in entries)
            {
                string[] parts = entry.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int value))
                {
                    result[parts[0]] = value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Helper method to deserialize a dictionary with float values from a string
        /// </summary>
        private Dictionary<string, float> DeserializeDictionaryFloat(string serialized)
        {
            Dictionary<string, float> result = new Dictionary<string, float>();
            
            if (string.IsNullOrEmpty(serialized))
            {
                return result;
            }
            
            string[] entries = serialized.Split('|');
            foreach (var entry in entries)
            {
                string[] parts = entry.Split(':');
                if (parts.Length == 2 && float.TryParse(parts[1], out float value))
                {
                    result[parts[0]] = value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Helper method to serialize a list to a string
        /// </summary>
        private string SerializeList(List<string> list)
        {
            if (list == null || list.Count == 0)
            {
                return string.Empty;
            }
            
            return string.Join("|", list);
        }
        
        /// <summary>
        /// Helper method to deserialize a list from a string
        /// </summary>
        private List<string> DeserializeList(string serialized)
        {
            List<string> result = new List<string>();
            
            if (string.IsNullOrEmpty(serialized))
            {
                return result;
            }
            
            string[] entries = serialized.Split('|');
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry))
                {
                    result.Add(entry);
                }
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Class that encapsulates tactical advice based on commander memory
    /// </summary>
    public class TacticalAdvice
    {
        // Basic tactical properties
        public float SuggestedAggression { get; set; } = 0.5f;
        public string PreferredFormationType { get; set; } = "Line";
        public string RecommendedTactic { get; set; } = "Balanced";
        public string PreferredTerrain { get; set; } = "Open";
        
        // Nemesis-related properties
        public bool HasVendettaAgainstPlayer { get; set; } = false;
        public string CommanderTitle { get; set; } = "";
        public bool HasLearningData { get; set; } = false;
        
        // Unit effectiveness
        public Dictionary<string, float> UnitEffectiveness { get; set; } = new Dictionary<string, float>();
        
        // Player knowledge
        public List<string> PlayerWeaknesses { get; set; } = new List<string>();
        public List<string> PlayerStrengths { get; set; } = new List<string>();
        
        // Constructor with default values
        public TacticalAdvice()
        {
            UnitEffectiveness = new Dictionary<string, float>
            {
                { "Infantry", 1.0f },
                { "Ranged", 1.0f },
                { "Cavalry", 1.0f },
                { "HorseArcher", 1.0f }
            };
        }
        
        /// <summary>
        /// Gets a description of the tactical advice for debugging purposes
        /// </summary>
        public override string ToString()
        {
            string result = $"Tactical Advice: {RecommendedTactic} (Aggression: {SuggestedAggression:P0})";
            
            if (HasVendettaAgainstPlayer && !string.IsNullOrEmpty(CommanderTitle))
            {
                result += $"\nCommander: {CommanderTitle}";
            }
            
            if (PlayerWeaknesses.Count > 0)
            {
                result += $"\nPlayer Weaknesses: {string.Join(", ", PlayerWeaknesses)}";
            }
            
            return result;
        }
    }
}