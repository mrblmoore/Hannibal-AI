using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public class MoveFormationCommand : AICommand
    {
        public Vec3 Destination { get; set; }

        public MoveFormationCommand(int formationIndex, Vec3 destination)
        {
            FormationIndex = formationIndex;
            TargetPosition = destination;
            Destination = destination;
        }
    }
}
