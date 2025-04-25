using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public class FlankCommand : AICommand
    {
        public Formation Formation { get; private set; }
        public Vec2 Direction { get; private set; }

        public FlankCommand(Formation formation, Vec2 direction)
            : base("Flank", CommandType.Flank)
        {
            Formation = formation;
            Direction = direction;
        }
    }
}
