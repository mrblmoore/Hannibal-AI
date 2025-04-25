using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public class FlankCommand
    {
        public Formation Formation { get; }
        public Formation TargetFormation { get; }
        public Vec3 FlankPosition { get; }

        public FlankCommand(Formation formation, Formation targetFormation, Vec3 flankPosition)
        {
            Formation = formation;
            TargetFormation = targetFormation;
            FlankPosition = flankPosition;
        }
    }
} 