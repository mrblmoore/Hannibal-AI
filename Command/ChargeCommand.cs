using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class ChargeCommand
    {
        public Formation Formation { get; }
        public Formation TargetFormation { get; }

        public ChargeCommand(Formation formation, Formation targetFormation)
        {
            Formation = formation;
            TargetFormation = targetFormation;
        }
    }
} 