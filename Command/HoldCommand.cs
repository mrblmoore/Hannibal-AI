using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public class HoldCommand
    {
        public Formation Formation { get; }
        public Vec3 Position { get; }

        public HoldCommand(Formation formation, Vec3 position)
        {
            Formation = formation;
            Position = position;
        }
    }
} 