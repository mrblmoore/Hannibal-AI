using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Memory
{
    /// <summary>
    /// Enhanced commander memory service with tactical adaptation capabilities and nemesis system
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
                }
                return _instance;
            }
        }
        
        // File paths for persistence
        private readonly string _memoryFilePath;
        
        // Commander personalities
        private Dictionary<string, CommanderPersonality> _commanderPersonalities;
        private string _currentCommanderId;
        private CommanderPersonality _currentPersonality;
        
        // Battle history
        private List<BattleRecord> _battleHistory;
        
        // Player analysis
        private PlayerAnalysis _playerAnalysis;
        
        // Nemesis System (enhanced commander tracking)
        private NemesisSystem _nemesisSystem;
        
        // Exposed properties
        public float AggressivenessScore => _currentPersonality?.Aggressiveness ?? 0.5f;
        public bool HasVendettaAgainstPlayer => _currentPersonality?.HasVendettaAgainstPlayer ?? false;
        public string CurrentCommanderType => _currentPersonality?.PersonalityType ?? "Unknown";
        
        // Nemesis system properties
        public NemesisCommander CurrentNemesis => _nemesisSystem?.GetCurrentNemesis();
        public bool HasActiveNemesis => CurrentNemesis != null;
        
        /// <summary>
        /// Initialize the commander memory service
        /// </summary>
        public CommanderMemoryService()
        {
            // Initialize collections
            _commanderPersonalities = new Dictionary<string, CommanderPersonality>();
            _battleHistory = new List<BattleRecord>();
            _playerAnalysis = new PlayerAnalysis();
            _nemesisSystem = new NemesisSystem();
            
            // Set file paths
            _memoryFilePath = Path.Combine(GetModuleRootPath(), "CommanderMemory.xml");
            
            // Create default personality
            _currentCommanderId = "default";
            _currentPersonality = CommanderPersonality.CreateRandom();
            _commanderPersonalities[_currentCommanderId] = _currentPersonality;
            
            Logger.Instance.Info("CommanderMemoryService initialized with Nemesis System");
            
            // Load any existing data
            LoadMemoryData();
        }
        
        /// <summary>
        /// Record a battle outcome and update commander personality
        /// </summary>
        public void RecordBattleOutcome(string battleId, string commanderId, bool victory, bool againstPlayer, 
                                        Dictionary<TacticType, float> tacticSuccessRates, 
                                        Dictionary<FormationClass, float> formationPerformance)
        {
            try
            {
                // Ensure we have a commander ID
                if (string.IsNullOrEmpty(commanderId))
                {
                    commanderId = _currentCommanderId;
                }
                
                // Get or create commander personality
                CommanderPersonality personality;
                if (!_commanderPersonalities.TryGetValue(commanderId, out personality))
                {
                    personality = CommanderPersonality.CreateRandom();
                    _commanderPersonalities[commanderId] = personality;
                }
                
                // Update commander personality
                personality.UpdateFromBattleOutcome(victory, againstPlayer, tacticSuccessRates);
                
                // Update formation effectiveness
                foreach (var performance in formationPerformance)
                {
                    personality.UpdateFormationEffectiveness(performance.Key, performance.Value);
                }
                
                // Create battle record
                BattleRecord record = new BattleRecord
                {
                    BattleId = battleId,
                    CommanderId = commanderId,
                    Timestamp = DateTime.Now,
                    Victory = victory,
                    AgainstPlayer = againstPlayer,
                    TacticSuccessRates = new Dictionary<TacticType, float>(tacticSuccessRates),
                    FormationPerformance = new Dictionary<FormationClass, float>(formationPerformance)
                };
                
                // Add to battle history
                _battleHistory.Add(record);
                
                // If against player, update player analysis
                if (againstPlayer)
                {
                    _playerAnalysis.UpdateFromBattle(record);
                }
                
                // Set as current if this is the current commander
                if (commanderId == _currentCommanderId)
                {
                    _currentPersonality = personality;
                }
                
                // Save updated data
                SaveMemoryData();
                
                Logger.Instance.Info($"Recorded battle outcome: Commander={commanderId}, Victory={victory}, AgainstPlayer={againstPlayer}");
                
                if (HasVendettaAgainstPlayer)
                {
                    Logger.Instance.Info("Commander has developed a vendetta against the player!");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error recording battle outcome: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Record a formation preference for the current commander
        /// </summary>
        public void RecordFormationPreference(string formationType)
        {
            if (string.IsNullOrEmpty(formationType))
            {
                return;
            }
            
            try
            {
                // Currently not used directly but could be enhanced to affect personality
                Logger.Instance.Info($"Recorded formation preference: {formationType}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error recording formation preference: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Record a tactical decision and its outcome
        /// </summary>
        public void RecordTacticalDecision(TacticType tacticType, bool successful, bool againstPlayer)
        {
            try
            {
                // Update tactic effectiveness
                if (_currentPersonality != null && _currentPersonality.TacticEffectiveness.ContainsKey(tacticType))
                {
                    float currentRating = _currentPersonality.TacticEffectiveness[tacticType];
                    float adjustment = successful ? 0.05f : -0.05f;
                    
                    // Apply smaller adjustment if learning rate should be lower
                    if (_currentPersonality.Adaptability < 0.5f)
                    {
                        adjustment *= _currentPersonality.Adaptability / 0.5f;
                    }
                    
                    // Update rating
                    _currentPersonality.TacticEffectiveness[tacticType] = 
                        Math.Max(0.1f, Math.Min(1.0f, currentRating + adjustment));
                    
                    Logger.Instance.Info($"Updated tactic effectiveness: {tacticType}={_currentPersonality.TacticEffectiveness[tacticType]:F2}");
                    
                    // Update player analysis if against player
                    if (againstPlayer)
                    {
                        if (successful)
                        {
                            _playerAnalysis.AddPlayerWeakness(tacticType.ToString());
                        }
                        else
                        {
                            _playerAnalysis.AddPlayerStrength(tacticType.ToString());
                        }
                    }
                    
                    // Save updated data
                    SaveMemoryData();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error recording tactical decision: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Record player unit composition for analysis
        /// </summary>
        public void RecordPlayerUnitComposition(Dictionary<FormationClass, int> unitCounts)
        {
            try
            {
                _playerAnalysis.UpdateUnitPreferences(unitCounts);
                
                Logger.Instance.Info("Recorded player unit composition");
                
                // Save updated data
                SaveMemoryData();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error recording player unit composition: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Set or create a commander personality for the current battle
        /// </summary>
        public void SetCurrentCommander(string commanderId)
        {
            try
            {
                if (string.IsNullOrEmpty(commanderId))
                {
                    commanderId = "default";
                }
                
                _currentCommanderId = commanderId;
                
                // Get or create commander personality
                if (!_commanderPersonalities.TryGetValue(commanderId, out _currentPersonality))
                {
                    _currentPersonality = CommanderPersonality.CreateRandom();
                    _commanderPersonalities[commanderId] = _currentPersonality;
                    
                    Logger.Instance.Info($"Created new commander personality: {commanderId} ({_currentPersonality.PersonalityType})");
                }
                else
                {
                    Logger.Instance.Info($"Set current commander to: {commanderId} ({_currentPersonality.PersonalityType})");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error setting current commander: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Generate tactical advice based on commander personality and player analysis
        /// </summary>
        public TacticalAdvice GetTacticalAdvice()
        {
            try
            {
                // Create default advice
                TacticalAdvice advice = new TacticalAdvice();
                
                if (_currentPersonality != null)
                {
                    // Set basic values from personality
                    advice.SuggestedAggression = _currentPersonality.Aggressiveness;
                    advice.HasVendettaAgainstPlayer = _currentPersonality.HasVendettaAgainstPlayer;
                    advice.CommanderTitle = _currentPersonality.PersonalityType;
                    
                    // Determine recommended tactic based on personality traits
                    advice.RecommendedTactic = DetermineRecommendedTactic(_currentPersonality);
                    
                    // Preferred terrain based on personality
                    advice.PreferredTerrain = DeterminePreferredTerrain(_currentPersonality);
                    
                    // Unit effectiveness based on personality and history
                    advice.UnitEffectiveness = GetUnitEffectiveness();
                    
                    // Set learning data flag if we have battle history
                    advice.HasLearningData = _currentPersonality.BattlesFought > 0;
                    
                    // Check if this is a nemesis commander
                    var currentNemesis = _nemesisSystem?.GetCurrentNemesis();
                    if (currentNemesis != null)
                    {
                        advice.IsNemesisCommander = true;
                        advice.PreviousEncounters = currentNemesis.EncounterCount;
                        advice.PreviousVictories = currentNemesis.VictoryCount;
                        advice.HasVendettaAgainstPlayer = currentNemesis.HasVendetta;
                        
                        // If nemesis has a more accurate title, use it
                        if (!string.IsNullOrEmpty(currentNemesis.CommanderName))
                        {
                            advice.CommanderTitle = $"{currentNemesis.CommanderName} ({currentNemesis.CommanderPersonality?.PersonalityType ?? "Unknown"})";
                        }
                        
                        Logger.Instance.Info($"Tactical advice includes nemesis data: {currentNemesis.CommanderName}, " +
                            $"Encounters: {currentNemesis.EncounterCount}, Vendetta: {currentNemesis.HasVendetta}");
                    }
                }
                
                // Add player weaknesses from analysis
                if (_playerAnalysis != null)
                {
                    advice.PlayerWeaknesses = _playerAnalysis.GetPlayerWeaknesses();
                    advice.PlayerStrengths = _playerAnalysis.GetPlayerStrengths();
                }
                
                return advice;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error generating tactical advice: {ex.Message}");
                return new TacticalAdvice();
            }
        }
        
        /// <summary>
        /// Get the most effective formation class based on history and personality
        /// </summary>
        public FormationClass GetMostEffectiveFormation()
        {
            FormationClass bestFormation = FormationClass.Infantry; // Default
            
            try
            {
                if (_currentPersonality != null && _currentPersonality.FormationEffectiveness.Count > 0)
                {
                    float highestEffectiveness = 0f;
                    
                    foreach (var formation in _currentPersonality.FormationEffectiveness)
                    {
                        if (formation.Value > highestEffectiveness)
                        {
                            highestEffectiveness = formation.Value;
                            bestFormation = formation.Key;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error getting most effective formation: {ex.Message}");
            }
            
            return bestFormation;
        }
        
        /// <summary>
        /// Get the recommended formation type for a specific formation class
        /// </summary>
        public string GetRecommendedFormationType(FormationClass formationClass)
        {
            try
            {
                if (_currentPersonality == null)
                {
                    return "Line"; // Default
                }
                
                // Base on personality traits
                switch (formationClass)
                {
                    case FormationClass.Infantry:
                        return _currentPersonality.PrefersDefense > 0.6f ? "ShieldWall" : "Line";
                        
                    case FormationClass.Ranged:
                        return "Loose";
                        
                    case FormationClass.Cavalry:
                        return _currentPersonality.Aggressiveness > 0.6f ? "Wedge" : "Column";
                        
                    case FormationClass.HorseArcher:
                        return "Loose";
                        
                    default:
                        return "Line";
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error getting recommended formation type: {ex.Message}");
                return "Line";
            }
        }
        
        /// <summary>
        /// Reset service for a new battle
        /// </summary>
        public void ResetForNewBattle()
        {
            try
            {
                // Generate a new random commander if we want variety
                if (ModConfig.Instance.Debug)
                {
                    Logger.Instance.Info("Creating new random commander personality for battle variety (debug mode)");
                    _currentPersonality = CommanderPersonality.CreateRandom();
                    _commanderPersonalities[_currentCommanderId] = _currentPersonality;
                }
                
                // Reset nemesis system for new battle
                if (_nemesisSystem != null)
                {
                    _nemesisSystem.ResetForNewBattle();
                }
                
                Logger.Instance.Info($"Reset for new battle with commander: {_currentPersonality.PersonalityType}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error resetting for new battle: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Identify a potential nemesis commander from the current battle
        /// </summary>
        /// <param name="commanderId">The unique identifier for the commander</param>
        /// <param name="commanderName">The display name of the commander</param>
        /// <param name="partyStrength">The military strength of the commander's party</param>
        public void IdentifyNemesisCommander(string commanderId, string commanderName, int partyStrength)
        {
            try
            {
                if (_nemesisSystem != null && !string.IsNullOrEmpty(commanderId))
                {
                    _nemesisSystem.IdentifyPotentialNemesis(commanderId, commanderName, partyStrength);
                    
                    // Get the current nemesis
                    var nemesis = _nemesisSystem.GetCurrentNemesis();
                    if (nemesis != null)
                    {
                        // If this is a returning nemesis, and they have a vendetta, display a message
                        if (nemesis.EncounterCount > 1 && nemesis.HasVendetta)
                        {
                            string message = $"{nemesis.CommanderName} remembers you! They are seeking revenge from your past encounters.";
                            InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
                            Logger.Instance.Info($"Nemesis with vendetta encountered: {nemesis.CommanderName}");
                            
                            // Incorporate nemesis personality into current commander
                            if (nemesis.CommanderPersonality != null)
                            {
                                // Blend their personality with our current defaults
                                _currentPersonality.Aggressiveness = Math.Max(_currentPersonality.Aggressiveness, nemesis.CommanderPersonality.Aggressiveness);
                                _currentPersonality.Stubbornness = Math.Max(_currentPersonality.Stubbornness, nemesis.CommanderPersonality.Stubbornness);
                                _currentPersonality.HasVendettaAgainstPlayer = true;
                                
                                Logger.Instance.Info("Applied nemesis personality traits to current battle commander");
                            }
                        }
                        else if (nemesis.EncounterCount > 1)
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                $"You face {nemesis.CommanderName} again!", Colors.Yellow));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error identifying nemesis commander: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Record a battle outcome for the nemesis system
        /// </summary>
        public void RecordNemesisBattle(string commanderId, bool playerVictory, 
            Dictionary<TacticType, float> tacticSuccessRates,
            Dictionary<FormationClass, float> formationPerformance)
        {
            try
            {
                if (_nemesisSystem != null && !string.IsNullOrEmpty(commanderId))
                {
                    _nemesisSystem.RecordNemesisBattleOutcome(
                        commanderId, 
                        playerVictory, 
                        tacticSuccessRates, 
                        formationPerformance);
                    
                    // Update our main data store and save
                    SaveMemoryData();
                    
                    // Show appropriate messages
                    if (!playerVictory)
                    {
                        // Player lost, nemesis won
                        var nemesis = _nemesisSystem.GetCurrentNemesis();
                        if (nemesis != null && nemesis.VictoryCount > 1)
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                $"{nemesis.CommanderName} has defeated you {nemesis.VictoryCount} times!", Colors.Red));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error recording nemesis battle: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get a list of all commanders with a vendetta against the player
        /// </summary>
        public List<NemesisCommander> GetCommandersWithVendetta()
        {
            try
            {
                if (_nemesisSystem != null)
                {
                    return _nemesisSystem.GetCommandersWithVendetta();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error getting commanders with vendetta: {ex.Message}");
            }
            
            return new List<NemesisCommander>();
        }
        
        #region Helper Methods
        
        /// <summary>
        /// Determine recommended tactic based on personality
        /// </summary>
        private string DetermineRecommendedTactic(CommanderPersonality personality)
        {
            // Find most effective tactic based on history
            TacticType bestTactic = TacticType.Defensive;
            float highestEffectiveness = 0f;
            
            foreach (var tactic in personality.TacticEffectiveness)
            {
                if (tactic.Value > highestEffectiveness)
                {
                    highestEffectiveness = tactic.Value;
                    bestTactic = tactic.Key;
                }
            }
            
            // If no clear winner from history, use personality
            if (highestEffectiveness < 0.6f)
            {
                if (personality.Aggressiveness > 0.7f)
                {
                    bestTactic = TacticType.Aggressive;
                }
                else if (personality.PrefersFlanking > 0.7f)
                {
                    bestTactic = TacticType.Flanking;
                }
                else if (personality.Caution > 0.7f)
                {
                    bestTactic = TacticType.Defensive;
                }
                else if (personality.Creativity > 0.7f)
                {
                    bestTactic = TacticType.Ambush;
                }
                else if (personality.HasVendettaAgainstPlayer)
                {
                    bestTactic = TacticType.Aggressive;
                }
            }
            
            return bestTactic.ToString();
        }
        
        /// <summary>
        /// Determine preferred terrain based on personality
        /// </summary>
        private string DeterminePreferredTerrain(CommanderPersonality personality)
        {
            if (personality.PrefersHighGround > 0.7f)
            {
                return "Elevated";
            }
            else if (personality.PrefersForestCover > 0.7f)
            {
                return "Forested";
            }
            else if (personality.PrefersOpenField > 0.7f)
            {
                return "Open";
            }
            else if (personality.PrefersCavalry > 0.7f)
            {
                return "Open";
            }
            else
            {
                return "Balanced";
            }
        }
        
        /// <summary>
        /// Get unit effectiveness based on personality and history
        /// </summary>
        private Dictionary<string, float> GetUnitEffectiveness()
        {
            Dictionary<string, float> effectiveness = new Dictionary<string, float>();
            
            try
            {
                if (_currentPersonality != null)
                {
                    // Convert from formation effectiveness
                    foreach (var formation in _currentPersonality.FormationEffectiveness)
                    {
                        effectiveness[formation.Key.ToString()] = formation.Value;
                    }
                    
                    // Adjust based on personality traits
                    if (_currentPersonality.PrefersCavalry > 0.7f)
                    {
                        if (effectiveness.ContainsKey("Cavalry"))
                        {
                            effectiveness["Cavalry"] = Math.Min(1.0f, effectiveness["Cavalry"] * 1.2f);
                        }
                    }
                    
                    if (_currentPersonality.PrefersRanged > 0.7f)
                    {
                        if (effectiveness.ContainsKey("Ranged"))
                        {
                            effectiveness["Ranged"] = Math.Min(1.0f, effectiveness["Ranged"] * 1.2f);
                        }
                    }
                }
                
                // Add defaults for missing entries
                if (!effectiveness.ContainsKey("Infantry")) effectiveness["Infantry"] = 0.5f;
                if (!effectiveness.ContainsKey("Ranged")) effectiveness["Ranged"] = 0.5f;
                if (!effectiveness.ContainsKey("Cavalry")) effectiveness["Cavalry"] = 0.5f;
                if (!effectiveness.ContainsKey("HorseArcher")) effectiveness["HorseArcher"] = 0.5f;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error getting unit effectiveness: {ex.Message}");
                
                // Set defaults
                effectiveness["Infantry"] = 0.5f;
                effectiveness["Ranged"] = 0.5f;
                effectiveness["Cavalry"] = 0.5f;
                effectiveness["HorseArcher"] = 0.5f;
            }
            
            return effectiveness;
        }
        
        /// <summary>
        /// Get root path of the mod
        /// </summary>
        private string GetModuleRootPath()
        {
            try
            {
                string moduleRoot = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(moduleRoot, "Modules", "HannibalAI");
            }
            catch
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }
        
        /// <summary>
        /// Load memory data from file
        /// </summary>
        private void LoadMemoryData()
        {
            try
            {
                Logger.Instance.Info($"Attempting to load commander memory from: {_memoryFilePath}");
                
                if (File.Exists(_memoryFilePath))
                {
                    using (StreamReader reader = new StreamReader(_memoryFilePath))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(CommanderMemoryData));
                        CommanderMemoryData data = (CommanderMemoryData)serializer.Deserialize(reader);
                        
                        // Apply loaded data
                        if (data != null)
                        {
                            _commanderPersonalities = data.CommanderPersonalities ?? new Dictionary<string, CommanderPersonality>();
                            _battleHistory = data.BattleHistory ?? new List<BattleRecord>();
                            _playerAnalysis = data.PlayerAnalysis ?? new PlayerAnalysis();
                            _currentCommanderId = data.CurrentCommanderId ?? "default";
                            
                            // Load nemesis system data if it exists
                            if (data.NemesisData != null)
                            {
                                _nemesisSystem = data.NemesisData;
                                Logger.Instance.Info($"Loaded nemesis system data with {_nemesisSystem.Commanders.Count} tracked enemy commanders");
                            }
                            
                            // Set current personality
                            if (!string.IsNullOrEmpty(_currentCommanderId) && _commanderPersonalities.ContainsKey(_currentCommanderId))
                            {
                                _currentPersonality = _commanderPersonalities[_currentCommanderId];
                            }
                            else
                            {
                                _currentPersonality = CommanderPersonality.CreateRandom();
                                _commanderPersonalities["default"] = _currentPersonality;
                                _currentCommanderId = "default";
                            }
                            
                            Logger.Instance.Info($"Loaded {_commanderPersonalities.Count} commander personalities and {_battleHistory.Count} battle records");
                        }
                    }
                }
                else
                {
                    Logger.Instance.Info("No existing commander memory file found, creating new memory");
                    
                    // Ensure we have a valid current personality
                    if (_currentPersonality == null)
                    {
                        _currentPersonality = CommanderPersonality.CreateRandom();
                        _commanderPersonalities[_currentCommanderId] = _currentPersonality;
                    }
                    
                    // Save initial data
                    SaveMemoryData();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Failed to load commander memory: {ex.Message}");
                
                // Ensure we have a valid current personality
                if (_currentPersonality == null)
                {
                    _currentPersonality = CommanderPersonality.CreateRandom();
                    _commanderPersonalities[_currentCommanderId] = _currentPersonality;
                }
            }
        }
        
        /// <summary>
        /// Save memory data to file
        /// </summary>
        private void SaveMemoryData()
        {
            try
            {
                // Create data container
                CommanderMemoryData data = new CommanderMemoryData
                {
                    CommanderPersonalities = _commanderPersonalities,
                    BattleHistory = _battleHistory,
                    PlayerAnalysis = _playerAnalysis,
                    CurrentCommanderId = _currentCommanderId,
                    NemesisData = _nemesisSystem
                };
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(_memoryFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Use direct XML serialization without XmlWriter settings
                using (StreamWriter writer = new StreamWriter(_memoryFilePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(CommanderMemoryData));
                    serializer.Serialize(writer, data);
                }
                
                Logger.Instance.Info($"Saved commander memory to: {_memoryFilePath}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Failed to save commander memory: {ex.Message}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Container for serializing all commander memory data
    /// </summary>
    [Serializable]
    public class CommanderMemoryData
    {
        public Dictionary<string, CommanderPersonality> CommanderPersonalities { get; set; } 
            = new Dictionary<string, CommanderPersonality>();
            
        public List<BattleRecord> BattleHistory { get; set; } 
            = new List<BattleRecord>();
            
        public PlayerAnalysis PlayerAnalysis { get; set; } 
            = new PlayerAnalysis();
            
        public string CurrentCommanderId { get; set; } 
            = "default";
            
        // Nemesis system data for enhanced commander tracking
        public NemesisSystem NemesisData { get; set; } 
            = new NemesisSystem();
    }
    
    /// <summary>
    /// Record of a battle's outcome and performance metrics
    /// </summary>
    [Serializable]
    public class BattleRecord
    {
        public string BattleId { get; set; }
        public string CommanderId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Victory { get; set; }
        public bool AgainstPlayer { get; set; }
        public Dictionary<TacticType, float> TacticSuccessRates { get; set; } 
            = new Dictionary<TacticType, float>();
        public Dictionary<FormationClass, float> FormationPerformance { get; set; } 
            = new Dictionary<FormationClass, float>();
    }
    
    /// <summary>
    /// Analysis of player behavior and weaknesses
    /// </summary>
    [Serializable]
    public class PlayerAnalysis
    {
        public int BattlesAgainstPlayer { get; set; } = 0;
        public int VictoriesAgainstPlayer { get; set; } = 0;
        
        public List<string> PlayerWeaknesses { get; set; } = new List<string>();
        public List<string> PlayerStrengths { get; set; } = new List<string>();
        
        public Dictionary<FormationClass, int> PreferredUnitTypes { get; set; } 
            = new Dictionary<FormationClass, int>();
            
        public Dictionary<string, float> TacticEffectivenessAgainstPlayer { get; set; } 
            = new Dictionary<string, float>();
        
        /// <summary>
        /// Update analysis based on battle outcome
        /// </summary>
        public void UpdateFromBattle(BattleRecord record)
        {
            if (!record.AgainstPlayer)
            {
                return;
            }
            
            BattlesAgainstPlayer++;
            
            if (record.Victory)
            {
                VictoriesAgainstPlayer++;
            }
            
            // Update tactic effectiveness
            foreach (var tactic in record.TacticSuccessRates)
            {
                string tacticName = tactic.Key.ToString();
                
                if (!TacticEffectivenessAgainstPlayer.ContainsKey(tacticName))
                {
                    TacticEffectivenessAgainstPlayer[tacticName] = 0.5f;
                }
                
                // Blend existing knowledge with new data
                TacticEffectivenessAgainstPlayer[tacticName] = 
                    (TacticEffectivenessAgainstPlayer[tacticName] * 0.7f) + (tactic.Value * 0.3f);
                
                // Identify strengths and weaknesses
                if (tactic.Value > 0.7f)
                {
                    AddPlayerWeakness(tacticName);
                }
                else if (tactic.Value < 0.3f)
                {
                    AddPlayerStrength(tacticName);
                }
            }
        }
        
        /// <summary>
        /// Update player unit preferences
        /// </summary>
        public void UpdateUnitPreferences(Dictionary<FormationClass, int> unitCounts)
        {
            foreach (var unitCount in unitCounts)
            {
                if (!PreferredUnitTypes.ContainsKey(unitCount.Key))
                {
                    PreferredUnitTypes[unitCount.Key] = 0;
                }
                
                PreferredUnitTypes[unitCount.Key] += unitCount.Value;
            }
        }
        
        /// <summary>
        /// Add a player weakness
        /// </summary>
        public void AddPlayerWeakness(string weakness)
        {
            if (!PlayerWeaknesses.Contains(weakness))
            {
                PlayerWeaknesses.Add(weakness);
                
                // If previously considered a strength, remove it
                if (PlayerStrengths.Contains(weakness))
                {
                    PlayerStrengths.Remove(weakness);
                }
            }
        }
        
        /// <summary>
        /// Add a player strength
        /// </summary>
        public void AddPlayerStrength(string strength)
        {
            if (!PlayerStrengths.Contains(strength))
            {
                PlayerStrengths.Add(strength);
                
                // If previously considered a weakness, remove it
                if (PlayerWeaknesses.Contains(strength))
                {
                    PlayerWeaknesses.Remove(strength);
                }
            }
        }
        
        /// <summary>
        /// Get list of player weaknesses (limited to top 3)
        /// </summary>
        public List<string> GetPlayerWeaknesses()
        {
            List<string> weaknesses = new List<string>();
            
            foreach (string weakness in PlayerWeaknesses)
            {
                weaknesses.Add(weakness);
                
                if (weaknesses.Count >= 3)
                {
                    break;
                }
            }
            
            return weaknesses;
        }
        
        /// <summary>
        /// Get list of player strengths (limited to top 3)
        /// </summary>
        public List<string> GetPlayerStrengths()
        {
            List<string> strengths = new List<string>();
            
            foreach (string strength in PlayerStrengths)
            {
                strengths.Add(strength);
                
                if (strengths.Count >= 3)
                {
                    break;
                }
            }
            
            return strengths;
        }
        
        /// <summary>
        /// Get player's most preferred formation class
        /// </summary>
        public FormationClass GetMostPreferredUnitType()
        {
            FormationClass mostPreferred = FormationClass.Infantry; // Default
            int highestCount = 0;
            
            foreach (var unitType in PreferredUnitTypes)
            {
                if (unitType.Value > highestCount)
                {
                    highestCount = unitType.Value;
                    mostPreferred = unitType.Key;
                }
            }
            
            return mostPreferred;
        }
    }
    
    /// <summary>
    /// Tactical advice based on commander memory and player analysis
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
        public bool IsNemesisCommander { get; set; } = false;
        public int PreviousEncounters { get; set; } = 0;
        public int PreviousVictories { get; set; } = 0;
        
        // Unit effectiveness
        public Dictionary<string, float> UnitEffectiveness { get; set; } = new Dictionary<string, float>();
        
        // Player knowledge
        public List<string> PlayerWeaknesses { get; set; } = new List<string>();
        public List<string> PlayerStrengths { get; set; } = new List<string>();
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public TacticalAdvice()
        {
            UnitEffectiveness = new Dictionary<string, float>
            {
                { "Infantry", 0.5f },
                { "Ranged", 0.5f },
                { "Cavalry", 0.5f },
                { "HorseArcher", 0.5f }
            };
        }
        
        /// <summary>
        /// Gets a description of the tactical advice for debugging purposes
        /// </summary>
        public override string ToString()
        {
            string result = $"Tactical Advice: {RecommendedTactic} (Aggression: {SuggestedAggression:P0})";
            
            if (IsNemesisCommander)
            {
                string nemesisInfo = $"\nNemesis Commander: {CommanderTitle}";
                
                if (PreviousEncounters > 0)
                {
                    nemesisInfo += $" - Encountered {PreviousEncounters} times, {PreviousVictories} victories";
                }
                
                if (HasVendettaAgainstPlayer)
                {
                    nemesisInfo += " - HAS VENDETTA AGAINST PLAYER";
                }
                
                result += nemesisInfo;
            }
            else if (HasVendettaAgainstPlayer && !string.IsNullOrEmpty(CommanderTitle))
            {
                result += $"\nCommander: {CommanderTitle} - Has vendetta against player";
            }
            
            if (PlayerWeaknesses.Count > 0)
            {
                result += $"\nPlayer Weaknesses: {string.Join(", ", PlayerWeaknesses)}";
            }
            
            if (PlayerStrengths.Count > 0)
            {
                result += $"\nPlayer Strengths: {string.Join(", ", PlayerStrengths)}";
            }
            
            return result;
        }
    }
}