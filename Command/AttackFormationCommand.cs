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

        public AttackFormationCommand(Vec3 targetPosition, int targetFormationId) 
            : base(targetPosition, CommandType.Attack)
        {
            TargetFormationId = targetFormationId;
        }

        public AttackFormationCommand(Formation formation, Formation targetFormation)
        {
            Formation = formation;
            TargetFormation = targetFormation;
        }

        public override string ToString()
        {
            return $"AttackFormationCommand: Target Formation {TargetFormationId}";
        }
    }
} 