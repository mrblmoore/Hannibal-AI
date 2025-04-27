using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class ChangeFormationCommand : AICommand
    {
        public Formation Formation { get; private set; }

        public ChangeFormationCommand(Formation formation)
            : base(CommandType.FormationChange)
        {
            Formation = formation;
        }
    }
}
