using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class ChangeFormationCommand : AICommand
    {
        public string NewFormationType { get; set; }
        public Formation Formation { get; }
        public Formation NewFormation { get; }

        public ChangeFormationCommand(Vec3 targetPosition, string newFormationType, Formation formation, Formation newFormation) 
            : base(targetPosition, CommandType.ChangeFormation)
        {
            NewFormationType = newFormationType;
            Formation = formation;
            NewFormation = newFormation;
        }

        public override string ToString()
        {
            return $"ChangeFormationCommand: New Formation Type {NewFormationType}";
        }
    }
} 