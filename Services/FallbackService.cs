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

        public FallbackService()
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
            if (snapshot == null)
            {
                return new AIDecision();
            }

            try
            {
                // Simple fallback logic: move towards the nearest enemy formation
                var commands = new List<AICommand>();
                var enemyFormations = snapshot.EnemyTeam?.Formations;
                var friendlyFormations = snapshot.FriendlyTeam?.Formations;

                if (enemyFormations != null && friendlyFormations != null)
                {
                    foreach (var formation in friendlyFormations)
                    {
                        var nearestEnemy = enemyFormations
                            .OrderBy(e => Vec3.Distance(formation.Position, e.Position))
                            .FirstOrDefault();

                        if (nearestEnemy != null)
                        {
                            commands.Add(new MoveFormationCommand(formation.Position, 1.0f));
                        }
                    }
                }

                return new AIDecision
                {
                    Action = "fallback_move",
                    Commands = commands.ToArray(),
                    Reasoning = "Fallback: Moving towards nearest enemy formation"
                };
            }
            catch (Exception ex)
            {
                Debug.Print($"Error in fallback decision: {ex.Message}");
                return new AIDecision();
            }
        }

        public AIDecision GetDecisionSync(BattleSnapshot snapshot)
        {
            return GetDecisionAsync(snapshot).GetAwaiter().GetResult();
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
                    new MoveFormationCommand(new Vec3(0, 0, 0), 1.0f)
                },
                Reasoning = "Emergency fallback - defaulting to defensive stance"
            };
        }
    }
} 