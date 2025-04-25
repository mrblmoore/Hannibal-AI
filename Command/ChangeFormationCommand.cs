using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class ChangeFormationCommand : AICommand
    {
        public Formation Formation { get; private set; }
        public FormOrder FormOrder { get; private set; }

        public ChangeFormationCommand(Formation formation, FormOrder formOrder)
            : base("Change Formation", CommandType.ChangeFormation)
        {
            Formation = formation;
            FormOrder = formOrder;
        }
    }
}
