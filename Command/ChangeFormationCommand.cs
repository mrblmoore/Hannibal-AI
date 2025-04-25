using System;

namespace HannibalAI.Command
{
    public class ChangeFormationCommand : AICommand
    {
        public int FormationId { get; set; }
        public float Width { get; set; }

        public ChangeFormationCommand(int formationId, float width)
        {
            FormationId = formationId;
            Width = width;
        }

        public override string ToString()
        {
            return $"ChangeFormationCommand: Formation {FormationId} width to {Width}";
        }
    }
} 