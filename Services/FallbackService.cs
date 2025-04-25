using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using HannibalAI.Battle;
using TaleWorlds.Library;
using HannibalAI.Command;
using TaleWorlds.Core;

namespace HannibalAI.Services
{
    public class FallbackService
    {
        private static FallbackService _instance;
        private readonly Random _random;

        private FallbackService()
        {
            _random = new Random();
        }

        public static FallbackService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FallbackService();
                }
                return _instance;
            }
        }

        public async Task<AIDecision> GetDecisionAsync(BattleSnapshot snapshot)
        {
            // For now, we'll just return a synchronous decision
            // In the future, you might want to make this truly asynchronous
            return GetDecision(snapshot);
        }

        private AIDecision GetDecision(BattleSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                {
                    return null;
                }

                // Simple strength comparison
                float playerStrength = CalculateTeamStrength(snapshot.PlayerUnits);
                float enemyStrength = CalculateTeamStrength(snapshot.EnemyUnits);
                float strengthRatio = enemyStrength / playerStrength;

                var commands = new List<AICommand>();

                // Basic decision making based on strength ratio
                if (strengthRatio > 1.2f)
                {
                    // Stronger - aggressive approach
                    commands.Add(new AICommand
                    {
                        Type = "formation",
                        Value = "line",
                        Parameters = new object[] { "main_force" }
                    });
                    commands.Add(new AICommand
                    {
                        Type = "movement",
                        Value = "advance",
                        Parameters = new object[] { "aggressive" }
                    });
                }
                else if (strengthRatio < 0.8f)
                {
                    // Weaker - defensive approach
                    commands.Add(new AICommand
                    {
                        Type = "formation",
                        Value = "shield_wall",
                        Parameters = new object[] { "main_force" }
                    });
                    commands.Add(new AICommand
                    {
                        Type = "movement",
                        Value = "hold",
                        Parameters = new object[] { "defensive" }
                    });
                }
                else
                {
                    // Even match - balanced approach
                    commands.Add(new AICommand
                    {
                        Type = "formation",
                        Value = "line",
                        Parameters = new object[] { "main_force" }
                    });
                    commands.Add(new AICommand
                    {
                        Type = "movement",
                        Value = "advance",
                        Parameters = new object[] { "cautious" }
                    });
                }

                // Add targeting command if ranged units present
                if (HasRangedUnits(snapshot.EnemyUnits))
                {
                    commands.Add(new AICommand
                    {
                        Type = "targeting",
                        Value = "focus_fire",
                        Parameters = new object[] { "enemy_infantry" }
                    });
                }

                return new AIDecision
                {
                    Action = strengthRatio > 1.2f ? "aggressive" : (strengthRatio < 0.8f ? "defensive" : "balanced"),
                    Commands = commands.ToArray(),
                    Reasoning = $"Fallback decision based on strength ratio: {strengthRatio:F2}"
                };
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[HannibalAI] Error in fallback service: {ex.Message}");
                return CreateEmergencyFallback();
            }
        }

        private float CalculateTeamStrength(List<UnitData> units)
        {
            if (units == null || units.Count == 0)
                return 0f;

            return units.Sum(u => u.Health / u.MaxHealth);
        }

        private bool HasRangedUnits(List<UnitData> units)
        {
            return units?.Any(u => u.IsRanged) ?? false;
        }

        private AIDecision CreateEmergencyFallback()
        {
            // Absolute fallback - basic defensive formation
            return new AIDecision
            {
                Action = "emergency_defensive",
                Commands = new[]
                {
                    new AICommand
                    {
                        Type = "formation",
                        Value = "shield_wall",
                        Parameters = new object[] { "main_force" }
                    },
                    new AICommand
                    {
                        Type = "movement",
                        Value = "hold",
                        Parameters = new object[] { "defensive" }
                    }
                },
                Reasoning = "Emergency fallback - defaulting to defensive stance"
            };
        }
    }
} 