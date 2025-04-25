using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class FollowCommand
    {
        public Formation Formation { get; }
        public Formation TargetFormation { get; }
        public float Distance { get; }

        public FollowCommand(Formation formation, Formation targetFormation, float distance)
        {
            Formation = formation;
            TargetFormation = targetFormation;
            Distance = distance;
        }
    }
} 