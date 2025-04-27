using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public class HoldCommand : AICommand
    {
        public Formation Formation { get; private set; }
        public Vec3 HoldPosition { get; private set; }

        public HoldCommand(Formation formation, Vec3 holdPosition)
            : base(CommandType.Movement)
        {
            Formation = formation;
            HoldPosition = holdPosition;
        }
    }
}
