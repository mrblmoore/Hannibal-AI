using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace HannibalAI.Memory
{
    /// <summary>
    /// Represents a commander's personality traits that influence tactical decisions
    /// </summary>
    [Serializable]
    public class CommanderPersonality
    {
        // Base personality traits (0.0 to 1.0)
        public float Aggressiveness { get; set; } = 0.5f;
        public float Caution { get; set; } = 0.5f;
        public float Creativity { get; set; } = 0.5f;
        public float Adaptability { get; set; } = 0.5f;
        public float Stubbornness { get; set; } = 0.5f;
        
        // Tactical preferences
        public float PrefersFlanking { get; set; } = 0.5f;
        public float PrefersDefense { get; set; } = 0.5f;
        public float PrefersRanged { get; set; } = 0.5f;
        public float PrefersCavalry { get; set; } = 0.5f;
        
        // Terrain preferences
        public float PrefersHighGround { get; set; } = 0.7f;
        public float PrefersForestCover { get; set; } = 0.5f;
        public float PrefersOpenField { get; set; } = 0.5f;
        
        // Experience and learning
        public int BattlesFought { get; set; } = 0;
        public int BattlesWon { get; set; } = 0;
        public int BattlesLost { get; set; } = 0;
        
        // Special traits
        public bool HasVendettaAgainstPlayer { get; set; } = false;
        public int ConsecutiveLossesToPlayer { get; set; } = 0;
        public string PersonalityType { get; set; } = "Balanced";
        
        // Formation effectiveness history
        public Dictionary<FormationClass, float> FormationEffectiveness { get; set; }
        
        // Tactics effectiveness history
        public Dictionary<TacticType, float> TacticEffectiveness { get; set; }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public CommanderPersonality()
        {
            // Initialize dictionaries
            FormationEffectiveness = new Dictionary<FormationClass, float>();
            TacticEffectiveness = new Dictionary<TacticType, float>();
            
            // Initialize formation effectiveness with neutral values
            foreach (FormationClass formationClass in Enum.GetValues(typeof(FormationClass)))
            {
                if (formationClass != FormationClass.NumberOfAllFormations)
                {
                    FormationEffectiveness[formationClass] = 0.5f;
                }
            }
            
            // Initialize tactic effectiveness with neutral values
            foreach (TacticType tacticType in Enum.GetValues(typeof(TacticType)))
            {
                TacticEffectiveness[tacticType] = 0.5f;
            }
        }
        
        /// <summary>
        /// Create a random personality
        /// </summary>
        public static CommanderPersonality CreateRandom()
        {
            Random random = new Random();
            CommanderPersonality personality = new CommanderPersonality();
            
            // Randomize base traits
            personality.Aggressiveness = (float)random.NextDouble();
            personality.Caution = (float)random.NextDouble();
            personality.Creativity = (float)random.NextDouble();
            personality.Adaptability = (float)random.NextDouble();
            personality.Stubbornness = (float)random.NextDouble();
            
            // Randomize tactical preferences
            personality.PrefersFlanking = (float)random.NextDouble();
            personality.PrefersDefense = (float)random.NextDouble();
            personality.PrefersRanged = (float)random.NextDouble();
            personality.PrefersCavalry = (float)random.NextDouble();
            
            // Randomize terrain preferences
            personality.PrefersHighGround = 0.5f + ((float)random.NextDouble() * 0.5f); // Always somewhat prefers high ground
            personality.PrefersForestCover = (float)random.NextDouble();
            personality.PrefersOpenField = (float)random.NextDouble();
            
            // Determine personality type based on dominant traits
            personality.DeterminePersonalityType();
            
            return personality;
        }
        
        /// <summary>
        /// Determine personality type based on traits
        /// </summary>
        public void DeterminePersonalityType()
        {
            if (Aggressiveness > 0.7f && PrefersCavalry > 0.6f)
            {
                PersonalityType = "Aggressive Cavalry Commander";
            }
            else if (Aggressiveness > 0.7f && PrefersRanged > 0.6f)
            {
                PersonalityType = "Aggressive Archer Commander";
            }
            else if (Caution > 0.7f && PrefersDefense > 0.6f)
            {
                PersonalityType = "Defensive Tactician";
            }
            else if (Creativity > 0.7f && Adaptability > 0.6f)
            {
                PersonalityType = "Adaptive Strategist";
            }
            else if (PrefersFlanking > 0.7f)
            {
                PersonalityType = "Flanking Specialist";
            }
            else if (Stubbornness > 0.7f)
            {
                PersonalityType = "Stubborn Commander";
            }
            else if (Aggressiveness > 0.6f)
            {
                PersonalityType = "Aggressive Commander";
            }
            else if (Caution > 0.6f)
            {
                PersonalityType = "Cautious Commander";
            }
            else
            {
                PersonalityType = "Balanced Commander";
            }
        }
        
        /// <summary>
        /// Update personality based on battle outcome
        /// </summary>
        public void UpdateFromBattleOutcome(bool victory, bool againstPlayer, Dictionary<TacticType, float> tacticSuccessRates)
        {
            // Update battle count
            BattlesFought++;
            
            if (victory)
            {
                BattlesWon++;
                
                // If we won, increase confidence in our current strategy
                Stubbornness += 0.05f;
                Caution -= 0.03f;
                
                // Reset player vendetta counter
                if (againstPlayer)
                {
                    ConsecutiveLossesToPlayer = 0;
                    HasVendettaAgainstPlayer = false;
                }
            }
            else
            {
                BattlesLost++;
                
                // If we lost, become more adaptable and cautious
                Adaptability += 0.05f;
                Caution += 0.05f;
                Stubbornness -= 0.03f;
                
                // Track losses against player
                if (againstPlayer)
                {
                    ConsecutiveLossesToPlayer++;
                    
                    // Develop vendetta after multiple losses
                    if (ConsecutiveLossesToPlayer >= 3)
                    {
                        HasVendettaAgainstPlayer = true;
                        Aggressiveness += 0.1f;
                    }
                }
            }
            
            // Update tactic effectiveness based on success rates
            foreach (var tacticRate in tacticSuccessRates)
            {
                if (TacticEffectiveness.ContainsKey(tacticRate.Key))
                {
                    // Blend current knowledge with new experience
                    TacticEffectiveness[tacticRate.Key] = (TacticEffectiveness[tacticRate.Key] * 0.7f) + (tacticRate.Value * 0.3f);
                }
            }
            
            // Clamp all values to valid range
            ClampPersonalityValues();
            
            // Recalculate personality type
            DeterminePersonalityType();
            
            Logger.Instance.Info($"Commander personality updated: {PersonalityType}, W/L: {BattlesWon}/{BattlesLost}");
            
            if (HasVendettaAgainstPlayer)
            {
                Logger.Instance.Info($"Commander has developed a vendetta against the player (Lost {ConsecutiveLossesToPlayer} times)");
            }
        }
        
        /// <summary>
        /// Update formation effectiveness based on battle performance
        /// </summary>
        public void UpdateFormationEffectiveness(FormationClass formationClass, float effectiveness)
        {
            if (FormationEffectiveness.ContainsKey(formationClass))
            {
                // Blend existing knowledge with new data
                FormationEffectiveness[formationClass] = (FormationEffectiveness[formationClass] * 0.7f) + (effectiveness * 0.3f);
                
                // Clamp to valid range
                FormationEffectiveness[formationClass] = Math.Max(0.1f, Math.Min(1.0f, FormationEffectiveness[formationClass]));
            }
        }
        
        /// <summary>
        /// Ensure all personality values stay within valid range
        /// </summary>
        private void ClampPersonalityValues()
        {
            Aggressiveness = Math.Max(0.1f, Math.Min(1.0f, Aggressiveness));
            Caution = Math.Max(0.1f, Math.Min(1.0f, Caution));
            Creativity = Math.Max(0.1f, Math.Min(1.0f, Creativity));
            Adaptability = Math.Max(0.1f, Math.Min(1.0f, Adaptability));
            Stubbornness = Math.Max(0.1f, Math.Min(1.0f, Stubbornness));
            
            PrefersFlanking = Math.Max(0.1f, Math.Min(1.0f, PrefersFlanking));
            PrefersDefense = Math.Max(0.1f, Math.Min(1.0f, PrefersDefense));
            PrefersRanged = Math.Max(0.1f, Math.Min(1.0f, PrefersRanged));
            PrefersCavalry = Math.Max(0.1f, Math.Min(1.0f, PrefersCavalry));
            
            PrefersHighGround = Math.Max(0.1f, Math.Min(1.0f, PrefersHighGround));
            PrefersForestCover = Math.Max(0.1f, Math.Min(1.0f, PrefersForestCover));
            PrefersOpenField = Math.Max(0.1f, Math.Min(1.0f, PrefersOpenField));
        }
        
        /// <summary>
        /// Generate a detailed description of this personality for debugging
        /// </summary>
        public string GetDetailedDescription()
        {
            return $"Commander Type: {PersonalityType}\n" +
                   $"Battle Record: {BattlesWon} wins, {BattlesLost} losses\n" +
                   $"Traits: Aggression {Aggressiveness:P0}, Caution {Caution:P0}, Adaptability {Adaptability:P0}\n" +
                   $"Tactical Preferences: Flanking {PrefersFlanking:P0}, Defense {PrefersDefense:P0}, Ranged {PrefersRanged:P0}, Cavalry {PrefersCavalry:P0}\n" +
                   $"Terrain Preferences: High Ground {PrefersHighGround:P0}, Forest {PrefersForestCover:P0}, Open Field {PrefersOpenField:P0}\n" +
                   (HasVendettaAgainstPlayer ? "Has vendetta against player!" : "");
        }
    }
    
    /// <summary>
    /// Types of tactics that can be employed
    /// </summary>
    public enum TacticType
    {
        Defensive,
        Aggressive,
        Flanking,
        Ambush,
        Skirmish,
        Charge,
        HoldPosition,
        Retreat,
        DoubleEnvelopment
    }
}