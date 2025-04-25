using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class ChangeFormationCommand
    {
        public Formation Formation { get; }
        public Formation NewFormation { get; }
        public string FormationType { get; }

        public ChangeFormationCommand(Formation formation, Formation newFormation, string formationType)
        {
            Formation = formation;
            NewFormation = newFormation;
            FormationType = formationType;
        }
    }
} 