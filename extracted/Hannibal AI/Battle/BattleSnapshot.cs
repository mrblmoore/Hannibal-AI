using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class BattleSnapshot
    {
        public List<UnitData> Units { get; private set; }
        public List<Formation> FriendlyFormations { get; private set; }
        public List<Formation> EnemyFormations { get; private set; }
        public WeatherData Weather { get; private set; }

        public BattleSnapshot(Mission mission)
        {
            Units = new List<UnitData>();
            FriendlyFormations = new List<Formation>();
            EnemyFormations = new List<Formation>();

            if (mission != null)
            {
                foreach (var agent in mission.Agents)
                {
                    if (agent.IsHuman && !agent.IsHero)
                    {
                        Units.Add(new UnitData(agent));
                    }
                }

                foreach (var team in mission.Teams)
                {
                    if (team.IsValid)
                    {
                        if (team.IsEnemyOf(mission.PlayerTeam))
                            EnemyFormations.AddRange(team.FormationsIncludingSpecial);
                        else
                            FriendlyFormations.AddRange(team.FormationsIncludingSpecial);
                    }
                }

                Weather = new WeatherData(mission.Scene);
            }
        }
    }
}
