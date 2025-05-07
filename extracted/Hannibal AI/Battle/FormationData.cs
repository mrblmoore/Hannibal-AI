using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class FormationData
    {
        public int Index { get; private set; }
        public Vec3 Position { get; private set; }
        public FormationClass FormationClass { get; private set; }
        public int CountOfUnits { get; private set; }

        public FormationData(Formation formation)
        {
            if (formation == null) return;

            Index = formation.Index;
            Position = formation.OrderPosition;
            FormationClass = formation.FormationIndex;
            CountOfUnits = formation.CountOfUnits;
        }
    }
}
