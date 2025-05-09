using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Memory
{
    /// <summary>
    /// Nemesis System that tracks persistent enemy commanders and evolves their behavior based on battle history
    /// </summary>
    [Serializable]
    public class NemesisSystem
    {
        // Collection of nemesis commanders that persist across battles
        [XmlArray("NemesisCommanders")]
        public List<NemesisCommander> Commanders { get; set; }
        
        // Current active nemesis in this battle
        [XmlIgnore]
        public NemesisCommander CurrentNemesis { get; private set; }
        
        // Enemy commanders from this battle session
        [XmlIgnore]
        private Dictionary<string, NemesisCommander> _sessionCommanders;
        
        // Enemy party groups from this battle
        [XmlIgnore]
        private List<string> _currentBattleParties;
        
        // Maximum number of nemesis commanders to track
        private const int MAX_NEMESIS_COMMANDERS = 10;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public NemesisSystem()
        {
            Commanders = new List<NemesisCommander>();
            _sessionCommanders = new Dictionary<string, NemesisCommander>();
            _currentBattleParties = new List<string>();
        }
        
        /// <summary>
        /// Identify an enemy commander from the current battle and potentially make them a nemesis
        /// </summary>
        public void IdentifyPotentialNemesis(string commanderId, string commanderName, int partyStrength)
        {
            try
            {
                // Skip if empty
                if (string.IsNullOrEmpty(commanderId))
                {
                    return;
                }
                
                // Check if this commander is already tracked
                NemesisCommander nemesis = Commanders.FirstOrDefault(c => c.CommanderId == commanderId);
                
                // If not found, create a new one
                if (nemesis == null)
                {
                    nemesis = new NemesisCommander
                    {
                        CommanderId = commanderId,
                        CommanderName = !string.IsNullOrEmpty(commanderName) ? commanderName : "Unknown Commander",
                        FirstEncounter = DateTime.Now,
                        LastEncounter = DateTime.Now,
                        PartyStrength = partyStrength,
                        EncounterCount = 1,
                        VictoryCount = 0,
                        DefeatCount = 0,
                        HasBeenDefeated = false,
                        IsDangerous = false,
                        CommanderPersonality = CommanderPersonality.CreateRandom(),
                        BattleTactics = new List<string>(),
                        PreferredFormations = new Dictionary<FormationClass, float>(),
                        AdaptationLevel = 0
                    };
                    
                    // Initialize formation preferences
                    foreach (FormationClass formation in Enum.GetValues(typeof(FormationClass)))
                    {
                        if (formation != FormationClass.NumberOfAllFormations)
                        {
                            nemesis.PreferredFormations[formation] = 0.5f;
                        }
                    }
                    
                    // Add to tracked commanders
                    Commanders.Add(nemesis);
                    
                    // Limit the number of commanders we track
                    if (Commanders.Count > MAX_NEMESIS_COMMANDERS)
                    {
                        // Remove the least encountered commander
                        NemesisCommander leastEncountered = Commanders
                            .OrderBy(c => c.EncounterCount)
                            .FirstOrDefault();
                            
                        if (leastEncountered != null)
                        {
                            Commanders.Remove(leastEncountered);
                        }
                    }
                    
                    Logger.Instance.Info($"New potential nemesis identified: {nemesis.CommanderName} ({nemesis.CommanderId})");
                }
                else
                {
                    // Update existing nemesis
                    nemesis.LastEncounter = DateTime.Now;
                    nemesis.EncounterCount++;
                    nemesis.PartyStrength = partyStrength; // Update with latest strength
                    
                    Logger.Instance.Info($"Encountered nemesis again: {nemesis.CommanderName} ({nemesis.CommanderId})," + 
                        $" Encounter #{nemesis.EncounterCount}");
                }
                
                // Add to current session commanders
                _sessionCommanders[commanderId] = nemesis;
                
                // Set as current nemesis
                CurrentNemesis = nemesis;
                
                // If the commander has been encountered multiple times, announce it
                if (nemesis.EncounterCount > 1)
                {
                    string encounterMessage = $"You face {nemesis.CommanderName} again!";
                    
                    if (nemesis.HasVendetta)
                    {
                        encounterMessage += " This commander has a vendetta against you!";
                    }
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        encounterMessage,
                        Colors.Yellow));
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error identifying potential nemesis: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Record a battle outcome against a specific nemesis
        /// </summary>
        public void RecordNemesisBattleOutcome(string commanderId, bool playerVictory, 
            Dictionary<TacticType, float> tacticSuccessRates,
            Dictionary<FormationClass, float> formationPerformance)
        {
            try
            {
                // Get the nemesis for this battle
                NemesisCommander nemesis = null;
                
                if (_sessionCommanders.TryGetValue(commanderId, out nemesis))
                {
                    if (playerVictory)
                    {
                        // Player won, nemesis lost
                        nemesis.DefeatCount++;
                        nemesis.HasBeenDefeated = true;
                        
                        // Increase adaptation level (they learn from defeat)
                        nemesis.AdaptationLevel += 1;
                        
                        // They may develop a vendetta after multiple defeats
                        if (nemesis.DefeatCount >= 2)
                        {
                            nemesis.HasVendetta = true;
                            
                            // Make their personality more aggressive and stubborn
                            nemesis.CommanderPersonality.Aggressiveness += 0.1f;
                            nemesis.CommanderPersonality.Stubbornness += 0.1f;
                            
                            // Adjust traits so they're more likely to seek revenge
                            if (nemesis.CommanderPersonality.Aggressiveness < 0.7f)
                            {
                                nemesis.CommanderPersonality.Aggressiveness = 0.7f;
                            }
                            
                            // Clamp values
                            nemesis.CommanderPersonality.ClampPersonalityValues();
                            
                            Logger.Instance.Info($"Nemesis {nemesis.CommanderName} has developed a vendetta!");
                        }
                    }
                    else
                    {
                        // Nemesis won, player lost
                        nemesis.VictoryCount++;
                        
                        // Increase danger level
                        if (nemesis.VictoryCount >= 2)
                        {
                            nemesis.IsDangerous = true;
                        }
                        
                        // Make slightly more confident in their approach
                        nemesis.CommanderPersonality.Stubbornness += 0.05f;
                        nemesis.CommanderPersonality.ClampPersonalityValues();
                    }
                    
                    // Update tactics and formation preferences
                    UpdateTacticsAndFormations(nemesis, tacticSuccessRates, formationPerformance, playerVictory);
                    
                    // Determine ranking based on history
                    UpdateNemesisRanking();
                    
                    Logger.Instance.Info($"Recorded battle outcome for nemesis {nemesis.CommanderName}: " + 
                        $"PlayerVictory={playerVictory}, Nemesis Record: {nemesis.VictoryCount}/{nemesis.DefeatCount}");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error recording nemesis battle outcome: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Update tactics and formation preferences based on battle results
        /// </summary>
        private void UpdateTacticsAndFormations(NemesisCommander nemesis, 
            Dictionary<TacticType, float> tacticSuccessRates,
            Dictionary<FormationClass, float> formationPerformance,
            bool playerVictory)
        {
            try
            {
                // If player won, nemesis should adapt more
                float adaptationStrength = playerVictory ? 0.4f : 0.2f;
                
                // Scale adaptation by their adaptation level (they get better at adapting)
                adaptationStrength *= (1.0f + (nemesis.AdaptationLevel * 0.1f));
                
                // Update tactic effectiveness
                foreach (var tacticRate in tacticSuccessRates)
                {
                    if (!playerVictory)
                    {
                        // If they won, reinforce successful tactics
                        if (tacticRate.Value > 0.6f)
                        {
                            string tacticName = tacticRate.Key.ToString();
                            if (!nemesis.BattleTactics.Contains(tacticName))
                            {
                                nemesis.BattleTactics.Add(tacticName);
                            }
                        }
                    }
                    
                    // Update personality based on tactic success
                    UpdatePersonalityFromTactic(nemesis.CommanderPersonality, tacticRate.Key, tacticRate.Value, adaptationStrength);
                }
                
                // Update formation preferences
                foreach (var formation in formationPerformance)
                {
                    if (nemesis.PreferredFormations.ContainsKey(formation.Key))
                    {
                        // Blend current preference with new performance data
                        float currentPreference = nemesis.PreferredFormations[formation.Key];
                        float newPreference = (currentPreference * (1 - adaptationStrength)) + 
                                              (formation.Value * adaptationStrength);
                        
                        // Update preference
                        nemesis.PreferredFormations[formation.Key] = Math.Max(0.1f, Math.Min(1.0f, newPreference));
                    }
                }
                
                // Recalculate personality type
                nemesis.CommanderPersonality.DeterminePersonalityType();
                
                Logger.Instance.Info($"Updated tactics and formations for nemesis {nemesis.CommanderName}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error updating tactics and formations: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Update personality based on tactic success
        /// </summary>
        private void UpdatePersonalityFromTactic(CommanderPersonality personality, TacticType tacticType, 
            float successRate, float adaptationStrength)
        {
            try
            {
                // Adapt personality based on tactic performance
                switch (tacticType)
                {
                    case TacticType.Aggressive:
                        personality.Aggressiveness += (successRate - 0.5f) * adaptationStrength;
                        break;
                    
                    case TacticType.Defensive:
                        personality.PrefersDefense += (successRate - 0.5f) * adaptationStrength;
                        personality.Caution += (successRate - 0.5f) * adaptationStrength;
                        break;
                    
                    case TacticType.Flanking:
                        personality.PrefersFlanking += (successRate - 0.5f) * adaptationStrength;
                        personality.Creativity += (successRate - 0.5f) * adaptationStrength;
                        break;
                    
                    case TacticType.Ambush:
                        personality.PrefersForestCover += (successRate - 0.5f) * adaptationStrength;
                        personality.Creativity += (successRate - 0.5f) * adaptationStrength;
                        break;
                    
                    case TacticType.Skirmish:
                        personality.PrefersRanged += (successRate - 0.5f) * adaptationStrength;
                        break;
                    
                    case TacticType.Charge:
                        personality.PrefersCavalry += (successRate - 0.5f) * adaptationStrength;
                        personality.Aggressiveness += (successRate - 0.5f) * adaptationStrength;
                        break;
                    
                    case TacticType.HoldPosition:
                        personality.PrefersDefense += (successRate - 0.5f) * adaptationStrength;
                        personality.Stubbornness += (successRate - 0.5f) * adaptationStrength;
                        break;
                    
                    case TacticType.DoubleEnvelopment:
                        personality.Creativity += (successRate - 0.5f) * adaptationStrength;
                        personality.PrefersFlanking += (successRate - 0.5f) * adaptationStrength;
                        break;
                }
                
                // Ensure all values stay in range
                personality.ClampPersonalityValues();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error updating personality from tactic: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Update the ranking and importance of all nemesis commanders
        /// </summary>
        private void UpdateNemesisRanking()
        {
            try
            {
                // Sort commanders by a combination of encounters and threat level
                Commanders = Commanders
                    .OrderByDescending(c => (c.VictoryCount * 3) + c.EncounterCount + (c.HasVendetta ? 5 : 0) + (c.IsDangerous ? 3 : 0))
                    .ToList();
                
                // Set the top 3 as important
                for (int i = 0; i < Commanders.Count && i < 3; i++)
                {
                    Commanders[i].IsImportant = true;
                }
                
                Logger.Instance.Info($"Updated nemesis rankings. Top nemesis: {Commanders.FirstOrDefault()?.CommanderName ?? "None"}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error updating nemesis rankings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get the current nemesis for the battle
        /// </summary>
        public NemesisCommander GetCurrentNemesis()
        {
            return CurrentNemesis;
        }
        
        /// <summary>
        /// Get the most dangerous nemesis based on history
        /// </summary>
        public NemesisCommander GetMostDangerousNemesis()
        {
            return Commanders
                .OrderByDescending(c => c.VictoryCount)
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Get all nemesis commanders with a vendetta
        /// </summary>
        public List<NemesisCommander> GetCommandersWithVendetta()
        {
            return Commanders
                .Where(c => c.HasVendetta)
                .ToList();
        }
        
        /// <summary>
        /// Reset for a new battle
        /// </summary>
        public void ResetForNewBattle()
        {
            _sessionCommanders.Clear();
            _currentBattleParties.Clear();
            CurrentNemesis = null;
        }
    }
    
    /// <summary>
    /// Represents a persistent enemy commander that evolves over time (Nemesis)
    /// </summary>
    [Serializable]
    public class NemesisCommander
    {
        // Identity
        public string CommanderId { get; set; }
        public string CommanderName { get; set; }
        
        // Encounter history
        public DateTime FirstEncounter { get; set; }
        public DateTime LastEncounter { get; set; }
        public int EncounterCount { get; set; }
        public int VictoryCount { get; set; }
        public int DefeatCount { get; set; }
        
        // Status flags
        public bool HasBeenDefeated { get; set; }
        public bool HasVendetta { get; set; }
        public bool IsDangerous { get; set; }
        public bool IsImportant { get; set; }
        
        // Military data
        public int PartyStrength { get; set; }
        public CommanderPersonality CommanderPersonality { get; set; }
        public List<string> BattleTactics { get; set; }
        public Dictionary<FormationClass, float> PreferredFormations { get; set; }
        
        // Learning and adaptation
        public int AdaptationLevel { get; set; }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public NemesisCommander()
        {
            BattleTactics = new List<string>();
            PreferredFormations = new Dictionary<FormationClass, float>();
        }
        
        /// <summary>
        /// Get a detailed description for debugging
        /// </summary>
        public string GetDetailedDescription()
        {
            return $"Commander: {CommanderName} ({CommanderId})\n" +
                   $"First encountered: {FirstEncounter.ToShortDateString()}, " +
                   $"Last encountered: {LastEncounter.ToShortDateString()}\n" +
                   $"Record: {VictoryCount} victories, {DefeatCount} defeats in {EncounterCount} encounters\n" +
                   $"Status: {(HasVendetta ? "Has vendetta" : "")} {(IsDangerous ? "Dangerous" : "")} " +
                   $"{(IsImportant ? "Important" : "")}\n" +
                   $"Personality: {CommanderPersonality.PersonalityType}\n" +
                   $"Adaptation Level: {AdaptationLevel}";
        }
    }
}