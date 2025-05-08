using System;
using System.IO;
using Newtonsoft.Json;
using TaleWorlds.Core;

namespace HannibalAI
{
    public class CommanderMemoryService
    {
        private static CommanderMemoryService _instance;
        public static CommanderMemoryService Instance => _instance ??= new CommanderMemoryService();

        private readonly string _memoryPath = Path.Combine("Modules", "HannibalAI", "commander_memory.json");
        private CommanderMemory _memory;

        public float AggressivenessScore => _memory?.Aggressiveness ?? 0.5f;

        private CommanderMemoryService()
        {
            LoadMemory();
        }

        public void RecordBattleOutcome(bool victory, int casualties, int enemyCasualties)
        {
            _memory.Battles++;
            _memory.Victories += victory ? 1 : 0;
            _memory.TotalCasualties += casualties;
            _memory.EnemyCasualties += enemyCasualties;
            _memory.Aggressiveness = CalculateAggressiveness();
            SaveMemory();
        }

        private float CalculateAggressiveness()
        {
            if (_memory.Battles == 0) return 0.5f;
            float winRate = (float)_memory.Victories / _memory.Battles;
            float casualtyRatio = _memory.TotalCasualties > 0 ?
                (float)_memory.EnemyCasualties / _memory.TotalCasualties : 1;
            return (winRate + casualtyRatio) / 2;
        }

        private void LoadMemory()
        {
            try
            {
                if (File.Exists(_memoryPath))
                {
                    string json = File.ReadAllText(_memoryPath);
                    _memory = JsonConvert.DeserializeObject<CommanderMemory>(json);
                }
                _memory ??= new CommanderMemory();
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
                string json = JsonConvert.SerializeObject(_memory, Formatting.Indented);
                File.WriteAllText(_memoryPath, json);
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