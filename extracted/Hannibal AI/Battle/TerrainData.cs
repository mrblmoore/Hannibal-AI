using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Battle
{
    public class TerrainData
    {
        public float AverageHeight { get; private set; }
        public bool HasWater { get; private set; }

        public TerrainData(Mission mission)
        {
            if (mission == null || mission.Scene == null)
                return;

            // Sample terrain height at the center of the map
            Vec2 center = new Vec2(mission.Scene.Width * 0.5f, mission.Scene.Height * 0.5f);
            AverageHeight = mission.Scene.GetTerrainHeightAtPosition(center);

            // Check for water near the center
            float waterLevel = mission.Scene.GetWaterLevelAtPosition(center, false);
            HasWater = waterLevel > 0.1f; // Threshold: minimal water level
        }
    }
}
