using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public class MoveFormationCommand : AICommand
    {
        public Formation Formation { get; private set; }
        public Vec3 TargetPosition { get; private set; }

        public MoveFormationCommand(Formation formation, Vec3 targetPosition)
            : base(CommandType.Movement)
        {
            Formation = formation;
            TargetPosition = targetPosition;
        }
    }
}
