using System.Collections.Generic;
using HannibalAI.Battle;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Services
{
    public class FallbackService
    {
        public FallbackService()
        {
            // Intentionally empty or you can add basic initialization if needed later
        }

        public void GetFallbackDecision()
        {
            // Basic fallback logic if AI cannot make a decision
            System.Diagnostics.Debug.WriteLine("FallbackService: No valid AI decision available. Executing fallback behavior.");
        }

        public List<Formation> GetEnemyFormations(Mission mission)
        {
            if (mission?.PlayerTeam?.EnemyTeams == null)
                return new List<Formation>();

            var enemyFormations = new List<Formation>();

            foreach (var team in mission.PlayerTeam.EnemyTeams)
            {
                if (team?.FormationsIncludingEmpty == null)
                    continue;

                enemyFormations.AddRange(team.FormationsIncludingEmpty);
            }

            return enemyFormations;
        }

        public List<Formation> GetFriendlyFormations(Mission mission)
        {
            if (mission?.PlayerTeam?.FormationsIncludingEmpty == null)
                return new List<Formation>();

            return new List<Formation>(mission.PlayerTeam.FormationsIncludingEmpty);
        }
    }
}
