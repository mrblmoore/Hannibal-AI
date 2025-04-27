using TaleWorlds.Library;

namespace HannibalAI.Command
{
    public abstract class AICommand
    {
        public int FormationIndex { get; set; }
        public Vec3 TargetPosition { get; set; }
    }
}
