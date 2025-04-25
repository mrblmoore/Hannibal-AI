using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class BattleSnapshot
    {
        public string CommanderId { get; private set; }
        public float TimeOfDay { get; private set; }
        public List<UnitSnapshot> Units { get; private set; }
        public TerrainData Terrain { get; private set; }
        public WeatherData Weather { get; private set; }

        public BattleSnapshot(Mission mission, string commanderId)
        {
            if (mission == null)
                return;

            CommanderId = commanderId ?? "UnknownCommander";

            TimeOfDay = mission.Scene.TimeOfDay;

            Terrain = new TerrainData(mission.Scene, Vec2.Zero);
            Weather = new WeatherData(mission.Scene);

            Units = new List<UnitSnapshot>();
            foreach (var agent in mission.Agents)
            {
                if (agent.IsHuman)
                {
                    Units.Add(new UnitSnapshot(agent));
                }
            }
        }
    }

    public class UnitSnapshot
    {
        public Vec2 Position { get; private set; }
        public float Health { get; private set; }
        public string Team { get; private set; }

        public UnitSnapshot(Agent agent)
        {
            Position = agent.Position.AsVec2;
            Health = agent.Health;
            Team = agent.Team?.Side.ToString() ?? "Unknown";
        }
    }
}
