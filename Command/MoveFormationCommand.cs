using System;
using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public class MoveFormationCommand : AICommand
    {
        public new Vec3 TargetPosition { get; set; }
        public float Speed { get; set; }

        public MoveFormationCommand(Vec3 targetPosition, float speed)
        {
            Type = "move";
            TargetPosition = targetPosition;
            Speed = speed;
            Decision = CommandType.Move;
        }

        public override string ToString()
        {
            return $"MoveFormationCommand: Target {TargetPosition} with speed {Speed}";
        }
    }
} 