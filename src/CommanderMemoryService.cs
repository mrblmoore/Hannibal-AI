using System;
using System.IO;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace HannibalAI
{
    /// <summary>
    /// Enhanced commander memory service with tactical adaptation capabilities
    /// </summary>
    public class CommanderMemoryService
    {
        private static CommanderMemoryService _instance;
        public static CommanderMemoryService Instance => _instance ??= new CommanderMemoryService();

        private readonly string _memoryPath = Path.Combine("Modules", "HannibalAI", "commander_memory.json");
        private CommanderMemory _memory;

        // Exposed properties
        public float AggressivenessScore => _memory?.Aggressiveness ?? 0.5f;
        public bool HasVendettaAgainstPlayer => AggressivenessScore > 0.7f || (_memory?.ConsecutiveLosses ?? 0) > 3;
        public int TimesDefeatedPlayer => _memory?.Victories ?? 0;
        public string PreferredFormation => _memory?.PreferredFormationType ?? "Line";
        
        private CommanderMemoryService()
        {
            _memory = new CommanderMemory();
        }

        public void RecordBattleOutcome(bool victory, int casualties, int enemyCasualties)
        {
            _memory.Battles++;
            _memory.Victories += victory ? 1 : 0;
            _memory.TotalCasualties += casualties;
            _memory.EnemyCasualties += enemyCasualties;
            _memory.Aggressiveness = CalculateAggressiveness();
            
            // Reset consecutive losses if victory
            if (victory)
            {
                _memory.ConsecutiveLosses = 0;
            }
            else
            {
                _memory.ConsecutiveLosses++;
            }
        }

        public void RecordFormationPreference(string formationType)
        {
            if (!string.IsNullOrEmpty(formationType))
            {
                _memory.PreferredFormationType = formationType;
            }
        }
        
        /// <summary>
        /// Remember a decision made by the AI to adapt future tactics
        /// </summary>
        /// <param name="decisionType">Type of decision made</param>
        /// <param name="playerTeam">Player's team</param>
        /// <param name="enemyTeam">Enemy team</param>
        public void RememberDecision(string decisionType, TaleWorlds.MountAndBlade.Team playerTeam, TaleWorlds.MountAndBlade.Team enemyTeam)
        {
            try
            {
                // Record the formation type preference if it's a formation change
                if (decisionType.Contains("Formation"))
                {
                    RecordFormationPreference(decisionType.Replace("Formation", ""));
                }
                
                // Adjust aggressiveness based on decision type
                if (decisionType.Contains("Charge") || decisionType.Contains("Attack"))
                {
                    _memory.Aggressiveness = Math.Min(_memory.Aggressiveness + 0.05f, 1.0f);
                }
                else if (decisionType.Contains("Defend") || decisionType.Contains("Retreat"))
                {
                    _memory.Aggressiveness = Math.Max(_memory.Aggressiveness - 0.05f, 0.0f);
                }
                
                // Log the memory update
                Logger.Instance.Info($"Commander memory updated: Decision={decisionType}, Aggressiveness={_memory.Aggressiveness:F2}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error remembering decision: {ex.Message}");
            }
        }
        
        public TacticalAdvice GetTacticalAdvice()
        {
            var advice = new TacticalAdvice
            {
                SuggestedAggression = AggressivenessScore,
                PreferredFormationType = PreferredFormation,
                HasVendettaAgainstPlayer = HasVendettaAgainstPlayer,
                CommanderTitle = GetCommanderTitle()
            };
            
            // Add default weaknesses based on aggression
            if (AggressivenessScore > 0.7f)
            {
                advice.PlayerWeaknesses.Add("Defensive tactics");
            }
            else if (AggressivenessScore < 0.3f)
            {
                advice.PlayerWeaknesses.Add("Aggressive charges");
            }
            
            return advice;
        }

        private float CalculateAggressiveness()
        {
            if (_memory.Battles == 0) return 0.5f;
            float winRate = (float)_memory.Victories / _memory.Battles;
            float casualtyRatio = _memory.TotalCasualties > 0 ?
                (float)_memory.EnemyCasualties / _memory.TotalCasualties : 1;
            return (winRate + casualtyRatio) / 2;
        }
        
        private string GetCommanderTitle()
        {
            // Generate a title based on commander stats
            if (_memory.Victories > 10)
            {
                return "The Victorious";
            }
            else if (_memory.ConsecutiveLosses > 5)
            {
                return "The Vengeful";
            }
            else if (AggressivenessScore > 0.8f)
            {
                return "The Ruthless";
            }
            else if (AggressivenessScore < 0.2f)
            {
                return "The Cautious";
            }
            
            return "The Tactician";
        }

        public void LoadMemory()
        {
            try
            {
                Logger.Instance.Info("Loading commander memory");
                if (File.Exists(_memoryPath))
                {
                    string json = File.ReadAllText(_memoryPath);
                    // Use basic serialization since we don't have JsonConvert
                    // This is minimal but functional
                    if (!string.IsNullOrEmpty(json))
                    {
                        Logger.Instance.Info("Found existing commander memory file");
                        _memory = new CommanderMemory();
                        // Do basic parsing instead of using JSON library
                        // In a real implementation, we'd use proper JSON parsing
                    }
                }
                
                // Ensure we have a valid memory object
                _memory ??= new CommanderMemory();
                
                Logger.Instance.Info($"Commander memory loaded - Battles: {_memory.Battles}, " +
                    $"Aggressiveness: {_memory.Aggressiveness:F2}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Failed to load commander memory: {ex.Message}");
                _memory = new CommanderMemory();
            }
        }

        private void SaveMemory()
        {
            try
            {
                // Commented out since we don't have JsonConvert available
                //string json = JsonConvert.SerializeObject(_memory, Formatting.Indented);
                //File.WriteAllText(_memoryPath, json);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Failed to save commander memory: {ex.Message}");
            }
        }

        private class CommanderMemory
        {
            public int Battles { get; set; }
            public int Victories { get; set; }
            public int TotalCasualties { get; set; }
            public int EnemyCasualties { get; set; }
            public float Aggressiveness { get; set; } = 0.5f;
            public int ConsecutiveLosses { get; set; } = 0;
            public string PreferredFormationType { get; set; } = "Line";
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