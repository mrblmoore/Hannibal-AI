using System;

namespace HannibalAI.Command
{
    public class AttackFormationCommand : AICommand
    {
        public int FormationId { get; set; }
        public int TargetFormationId { get; set; }

        public AttackFormationCommand(int formationId, int targetFormationId)
        {
            FormationId = formationId;
            TargetFormationId = targetFormationId;
        }

        public override string ToString()
        {
            return $"AttackFormationCommand: Formation {FormationId} attacks {TargetFormationId}";
        }
    }
} 