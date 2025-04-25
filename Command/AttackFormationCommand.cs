using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class AttackFormationCommand : AICommand
    {
        public int TargetFormationId { get; set; }
        public Formation Formation { get; }
        public Formation TargetFormation { get; }

        public AttackFormationCommand(Formation formation, Formation targetFormation)
            : base("attack", (int)CommandType.Attack)
        {
            Formation = formation;
            TargetFormation = targetFormation;
            TargetFormationId = targetFormation?.Index ?? -1;
        }

        public override string ToString()
        {
            return $"AttackFormationCommand: Target Formation {TargetFormationId}";
        }
    }
} 