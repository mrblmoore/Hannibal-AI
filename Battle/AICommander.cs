using TaleWorlds.MountAndBlade;
using HannibalAI.Services;
using HannibalAI.Command;
using System;

namespace HannibalAI.Battle
{
    public class AICommander
    {
        private readonly FallbackService _fallbackService;

        public AICommander(FallbackService fallbackService)
        {
            _fallbackService = fallbackService;
        }

        public void MoveFormation(int formationIndex, Vec3 position)
        {
            // Implementation for moving formation
        }

        public void ChangeFormation(int formationIndex, FormationOrder formOrder)
        {
            // Implementation for changing formation
        }

        public void FlankEnemy(int formationIndex, Vec3 targetPosition)
        {
            // Implementation for flanking
        }

        public void HoldPosition(int formationIndex, Vec3 position)
        {
            // Implementation for holding position
        }

        public void ChargeFormation(int formationIndex)
        {
            // Implementation for charging
        }

        public void FollowTarget(int followerIndex, int leaderIndex)
        {
            // Implementation for following target
        }

        // [Future Feature] Dynamic AI per formation — currently unused.
        /*
        public void MakeDecision(Formation formation, Mission mission)
        {
            if (formation == null || mission == null)
                return;

            var snapshot = new BattleSnapshot(mission, formation.Team?.Side.ToString() ?? "Unknown");
            var decision = _fallbackService.GetFallbackDecision(snapshot);

            ExecuteCommand(decision, formation);
        }
        */

        private void ExecuteCommand(AIDecision decision, Formation formation)
        {
            // Placeholder for executing a fallback decision
        }
    }
}
