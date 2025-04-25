using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using HannibalAI.Config;

namespace HannibalAI.Services
{
    public class CommanderMemoryService
    {
        private static CommanderMemoryService _instance;
        private static readonly object _lock = new object();
        private readonly Dictionary<string, CommanderMemory> _commanderMemories;
        private readonly ModConfig _config;
        private readonly Dictionary<string, CommanderProfile> _profiles;
        private readonly string _savePath;
        private readonly Dictionary<string, float> _memories;
        private readonly Dictionary<string, DateTime> _lastUpdated;
        private const int MAX_BATTLES_PER_COMMANDER = 10;

        private CommanderMemoryService()
        {
            _commanderMemories = new Dictionary<string, CommanderMemory>();
            _config = ModConfig.Instance;
            _profiles = new Dictionary<string, CommanderProfile>();
            _savePath = Path.Combine(BasePath.Name, "Modules", "HannibalAI", "CommanderProfiles");
            _memories = new Dictionary<string, float>();
            _lastUpdated = new Dictionary<string, DateTime>();
            LoadProfiles();
        }

        public static CommanderMemoryService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CommanderMemoryService();
                        }
                    }
                }
                return _instance;
            }
        }

        public float GetMemoryValue(string key)
        {
            if (!_memories.ContainsKey(key))
            {
                return _config.CommanderMemoryMinValue;
            }

            var value = _memories[key];
            var lastUpdate = _lastUpdated[key];
            var timeSinceUpdate = (DateTime.UtcNow - lastUpdate).TotalSeconds;

            // Apply decay based on time since last update
            if (timeSinceUpdate > 0)
            {
                value = Math.Max(
                    _config.CommanderMemoryMinValue,
                    value - (_config.CommanderMemoryDecayRate * (float)timeSinceUpdate)
                );
                _memories[key] = value;
            }

            return value;
        }

        public void UpdateMemory(string key, float value)
        {
            value = Math.Min(_config.CommanderMemoryMaxValue, Math.Max(_config.CommanderMemoryMinValue, value));
            _memories[key] = value;
            _lastUpdated[key] = DateTime.UtcNow;

            // Clean up old memories
            CleanupOldMemories();
        }

        private void CleanupOldMemories()
        {
            var keysToRemove = new List<string>();
            var now = DateTime.UtcNow;

            foreach (var kvp in _lastUpdated)
            {
                if ((now - kvp.Value).TotalSeconds > _config.CommanderMemoryDuration)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _memories.Remove(key);
                _lastUpdated.Remove(key);
            }
        }

        public void UpdateCommanderMemory(string commanderId, float value)
        {
            if (!_commanderMemories.ContainsKey(commanderId))
            {
                _commanderMemories[commanderId] = new CommanderMemory
                {
                    LastUpdateTime = CampaignTime.Now,
                    Value = value
                };
            }
            else
            {
                var memory = _commanderMemories[commanderId];
                memory.Value = Math.Min(_config.CommanderMemoryMaxValue, 
                    Math.Max(_config.CommanderMemoryMinValue, value));
                memory.LastUpdateTime = CampaignTime.Now;
            }
        }

        public float GetCommanderMemory(string commanderId)
        {
            if (_commanderMemories.TryGetValue(commanderId, out var memory))
            {
                var timeSinceLastUpdate = CampaignTime.Now.ToHours - memory.LastUpdateTime.ToHours;
                if (timeSinceLastUpdate > _config.CommanderMemoryDuration)
                {
                    _commanderMemories.Remove(commanderId);
                    return 0f;
                }

                var decayedValue = memory.Value * 
                    (1f - (_config.CommanderMemoryDecayRate * timeSinceLastUpdate));
                return Math.Max(_config.CommanderMemoryMinValue, decayedValue);
            }
            return 0f;
        }

        public void ClearCommanderMemory(string commanderId)
        {
            _commanderMemories.Remove(commanderId);
        }

        public void ClearAllMemories()
        {
            _commanderMemories.Clear();
        }

        public CommanderProfile GetCommanderProfile(string commanderId)
        {
            if (string.IsNullOrEmpty(commanderId))
                return null;

            if (!_profiles.ContainsKey(commanderId))
            {
                _profiles[commanderId] = new CommanderProfile
                {
                    CommanderId = commanderId,
                    BattleHistory = new List<BattleRecord>(),
                    Traits = new Dictionary<string, float>
                    {
                        { "Aggression", 0.5f },
                        { "Caution", 0.5f },
                        { "Innovation", 0.5f },
                        { "Adaptability", 0.5f }
                    }
                };
            }

            return _profiles[commanderId];
        }

        public List<BattleRecord> GetRecentBattles(string commanderId, int count)
        {
            var profile = GetCommanderProfile(commanderId);
            return profile?.BattleHistory
                .OrderByDescending(b => b.Timestamp)
                .Take(count)
                .ToList() ?? new List<BattleRecord>();
        }

        public void RecordBattleOutcome(string commanderId, BattleOutcome outcome)
        {
            try
            {
                var profile = GetCommanderProfile(commanderId);
                if (profile == null) return;

                profile.BattleHistory.Add(new BattleRecord
                {
                    Timestamp = DateTime.UtcNow,
                    Outcome = outcome.Result,
                    PlayerCasualties = outcome.PlayerCasualties,
                    EnemyCasualties = outcome.EnemyCasualties,
                    TacticsUsed = outcome.TacticsUsed
                });

                // Keep only the most recent battles
                if (profile.BattleHistory.Count > MAX_BATTLES_PER_COMMANDER)
                {
                    profile.BattleHistory = profile.BattleHistory
                        .OrderByDescending(b => b.Timestamp)
                        .Take(MAX_BATTLES_PER_COMMANDER)
                        .ToList();
                }

                // Update commander traits based on battle outcome
                UpdateTraits(profile, outcome);

                SaveProfiles();
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"[HannibalAI] Error recording battle outcome: {ex.Message}"));
            }
        }

        private void UpdateTraits(CommanderProfile profile, BattleOutcome outcome)
        {
            // Adjust traits based on battle outcome and tactics used
            if (outcome.Result == "Victory")
            {
                // Successful aggressive tactics increase aggression
                if (outcome.TacticsUsed.Contains("charge") || outcome.TacticsUsed.Contains("flank"))
                {
                    AdjustTrait(profile.Traits, "Aggression", 0.1f);
                }
                // Successful defensive tactics increase caution
                else if (outcome.TacticsUsed.Contains("hold") || outcome.TacticsUsed.Contains("shield_wall"))
                {
                    AdjustTrait(profile.Traits, "Caution", 0.1f);
                }
            }
            else if (outcome.Result == "Defeat")
            {
                // Failed aggressive tactics decrease aggression
                if (outcome.TacticsUsed.Contains("charge") || outcome.TacticsUsed.Contains("flank"))
                {
                    AdjustTrait(profile.Traits, "Aggression", -0.1f);
                }
                // Failed defensive tactics decrease caution
                else if (outcome.TacticsUsed.Contains("hold") || outcome.TacticsUsed.Contains("shield_wall"))
                {
                    AdjustTrait(profile.Traits, "Caution", -0.1f);
                }
            }
        }

        private void AdjustTrait(Dictionary<string, float> traits, string trait, float adjustment)
        {
            if (traits.ContainsKey(trait))
            {
                traits[trait] = Math.Max(0f, Math.Min(1f, traits[trait] + adjustment));
            }
        }

        private void LoadProfiles()
        {
            try
            {
                if (!Directory.Exists(_savePath))
                {
                    Directory.CreateDirectory(_savePath);
                    return;
                }

                foreach (var file in Directory.GetFiles(_savePath, "*.json"))
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonConvert.DeserializeObject<CommanderProfile>(json);
                    if (profile != null && !string.IsNullOrEmpty(profile.CommanderId))
                    {
                        _profiles[profile.CommanderId] = profile;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error loading commander profiles: {ex.Message}");
            }
        }

        private void SaveProfiles()
        {
            try
            {
                if (!Directory.Exists(_savePath))
                {
                    Directory.CreateDirectory(_savePath);
                }

                foreach (var profile in _profiles.Values)
                {
                    var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                    var filePath = Path.Combine(_savePath, $"{profile.CommanderId}.json");
                    File.WriteAllText(filePath, json);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error saving commander profiles: {ex.Message}");
            }
        }

        private class CommanderMemory
        {
            public CampaignTime LastUpdateTime { get; set; }
            public float Value { get; set; }
        }
    }

    public class CommanderProfile
    {
        public string CommanderId { get; set; }
        public List<BattleRecord> BattleHistory { get; set; }
        public Dictionary<string, float> Traits { get; set; }
    }

    public class BattleRecord
    {
        public DateTime Timestamp { get; set; }
        public string Outcome { get; set; }
        public int PlayerCasualties { get; set; }
        public int EnemyCasualties { get; set; }
        public List<string> TacticsUsed { get; set; }
    }

    public class BattleOutcome
    {
        public string Result { get; set; }
        public int PlayerCasualties { get; set; }
        public int EnemyCasualties { get; set; }
        public List<string> TacticsUsed { get; set; }
    }
} 