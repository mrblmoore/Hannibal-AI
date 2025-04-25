using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using HannibalAI.Battle;
using TaleWorlds.Library;
using HannibalAI.Command;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Services
{
    public class FallbackService
    {
        private static FallbackService _instance;
        private readonly Random _random;
        private readonly Mission _mission;

        public FallbackService(Mission mission)
        {
            _random = new Random();
            _mission = mission;
        }

        public static FallbackService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FallbackService(null);
                }
                return _instance;
            }
        }

        public async Task<AIDecision> GetFallbackDecision(BattleSnapshot snapshot)
        {
            if (snapshot == null || _mission == null) return null;

            try
            {
                // Get player team formations
                var playerTeam = _mission.PlayerTeam;
                if (playerTeam == null) return null;

                var playerFormations = new List<Formation>();
                foreach (var formation in playerTeam.FormationsIncludingEmpty)
                {
                    if (formation != null && formation.CountOfUnits > 0)
                    {
                        playerFormations.Add(formation);
                    }
                }

                // Get enemy team formations
                var enemyTeam = _mission.PlayerEnemyTeam;
                if (enemyTeam == null) return null;

                var enemyFormations = new List<Formation>();
                foreach (var formation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (formation != null && formation.CountOfUnits > 0)
                    {
                        enemyFormations.Add(formation);
                    }
                }

                // TODO: Implement fallback decision logic using valid Bannerlord APIs
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetFallbackDecision: {ex.Message}");
                return null;
            }
        }

        public AIDecision GetDecisionSync(BattleSnapshot snapshot)
        {
            return GetFallbackDecision(snapshot).GetAwaiter().GetResult();
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