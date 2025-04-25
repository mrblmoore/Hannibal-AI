using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public class HoldCommand : AICommand
    {
        public Formation Formation { get; private set; }
        public Vec2 Position { get; private set; }

        public HoldCommand(Formation formation, Vec2 position)
            : base("Hold Position", CommandType.Hold)
        {
            Formation = formation;
            Position = position;
        }
    }
}
