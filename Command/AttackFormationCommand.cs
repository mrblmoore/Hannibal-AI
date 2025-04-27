using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class AttackFormationCommand : AICommand
    {
        public Formation Attacker { get; private set; }
        public Formation Target { get; private set; }

        public AttackFormationCommand(Formation attacker, Formation target)
            : base(CommandType.Engagement)
        {
            Attacker = attacker;
            Target = target;
        }
    }
}
