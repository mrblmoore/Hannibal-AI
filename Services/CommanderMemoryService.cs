using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace HannibalAI.Services
{
    public class CommanderMemoryService
    {
        private readonly Dictionary<string, CommanderProfile> _profiles;
        private readonly string _savePath;
        private const int MAX_BATTLES_PER_COMMANDER = 10;

        public CommanderMemoryService()
        {
            _profiles = new Dictionary<string, CommanderProfile>();
            _savePath = Path.Combine(BasePath.Name, "Modules", "HannibalAI", "CommanderProfiles");
            LoadProfiles();
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
                Debug.Print($"[HannibalAI] Error recording battle outcome: {ex.Message}");
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
                    AdjustTrait(profile.Traits, "Innovation", 0.05f);
                }

                // Successful defensive tactics increase caution
                if (outcome.TacticsUsed.Contains("shield_wall") || outcome.TacticsUsed.Contains("hold"))
                {
                    AdjustTrait(profile.Traits, "Caution", 0.1f);
                    AdjustTrait(profile.Traits, "Adaptability", 0.05f);
                }
            }
            else
            {
                // Failed aggressive tactics decrease aggression
                if (outcome.TacticsUsed.Contains("charge") || outcome.TacticsUsed.Contains("flank"))
                {
                    AdjustTrait(profile.Traits, "Aggression", -0.1f);
                    AdjustTrait(profile.Traits, "Caution", 0.1f);
                }

                // Failed defensive tactics decrease caution
                if (outcome.TacticsUsed.Contains("shield_wall") || outcome.TacticsUsed.Contains("hold"))
                {
                    AdjustTrait(profile.Traits, "Caution", -0.1f);
                    AdjustTrait(profile.Traits, "Innovation", 0.1f);
                }
            }
        }

        private void AdjustTrait(Dictionary<string, float> traits, string trait, float adjustment)
        {
            if (traits.ContainsKey(trait))
            {
                traits[trait] = Math.Clamp(traits[trait] + adjustment, 0f, 1f);
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