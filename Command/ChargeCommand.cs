using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class ChargeCommand : AICommand
    {
        public Formation Formation { get; private set; }

        public ChargeCommand(Formation formation)
            : base("Charge", CommandType.Charge)
        {
            Formation = formation;
        }
    }
}
