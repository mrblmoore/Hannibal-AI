using System;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using HannibalAI.Services;

namespace HannibalAI.Battle
{
    public class AICommander
    {
        private readonly Mission _mission;
        private readonly FallbackService _fallbackService;
        private BattleSnapshot _lastSnapshot;

        public AICommander(Mission mission)
        {
            _mission = mission;
            _fallbackService = new FallbackService(mission);
        }

        public async Task<AIDecision> GetDecision(BattleSnapshot snapshot)
        {
            try
            {
                _lastSnapshot = snapshot;
                return await _fallbackService.GetFallbackDecision(snapshot);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error getting AI decision: {ex.Message}");
                return null;
            }
        }

        public async Task<AIDecision> GetFallbackDecision()
        {
            try
            {
                return await _fallbackService.GetFallbackDecision(_lastSnapshot);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error getting fallback decision: {ex.Message}");
                return null;
            }
        }
    }
} 