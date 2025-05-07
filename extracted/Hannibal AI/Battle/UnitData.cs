using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class UnitData
    {
        public int Id { get; set; }
        public Vec3 Position { get; set; }
        public Vec2 Direction { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public int Team { get; set; }
        public int FormationIndex { get; set; }
        public bool IsMounted { get; set; }
        public bool IsCommander { get; set; }

        public UnitData(Agent agent)
        {
            if (agent == null)
                return;

            Id = agent.Index;
            Position = agent.Position;
            Direction = agent.LookDirection.ToVec2();
            Health = agent.Health;
            MaxHealth = agent.HealthLimit;
            Team = agent.Team?.Index ?? -1;
            FormationIndex = agent.Formation?.Index ?? -1;
            IsMounted = agent.HasMount;
            IsCommander = agent.IsPlayerControlled; // or agent.Character?.IsHero ?? false if you want AI commanders too
        }
    }
}
