using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public class HoldCommand : AICommand
    {
        public Vec3 HoldPosition { get; set; }

        public HoldCommand(int formationIndex, Vec3 holdPosition)
        {
            FormationIndex = formationIndex;
            HoldPosition = holdPosition;
        }
    }
}
