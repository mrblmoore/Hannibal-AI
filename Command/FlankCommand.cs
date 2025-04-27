using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public class FlankCommand : AICommand
    {
        public Vec3 TargetPosition { get; set; }

        public FlankCommand(int formationIndex, Vec3 targetPosition)
        {
            FormationIndex = formationIndex;
            TargetPosition = targetPosition;
        }
    }
}
