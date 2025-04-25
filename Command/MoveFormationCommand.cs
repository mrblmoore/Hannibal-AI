using System;
using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public class MoveFormationCommand : AICommand
    {
        public int FormationId { get; set; }
        public Vec3 TargetPosition { get; set; }

        public MoveFormationCommand(int formationId, Vec3 targetPosition)
        {
            FormationId = formationId;
            TargetPosition = targetPosition;
        }

        public override string ToString()
        {
            return $"MoveFormationCommand: Formation {FormationId} to {TargetPosition}";
        }
    }
} 