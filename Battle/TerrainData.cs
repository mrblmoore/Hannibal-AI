using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class TerrainData
    {
        public float Height { get; private set; }
        public float WaterLevel { get; private set; }
        public bool IsWater { get; private set; }

        public TerrainData(Scene scene, Vec2 position)
        {
            if (scene == null)
            {
                Height = 0f;
                WaterLevel = 0f;
                IsWater = false;
                return;
            }

            // Correct call for Bannerlord 1.2.12
            Height = scene.GetTerrainHeightAtPosition(position);

            // Corrected Scene method call
            WaterLevel = scene.GetWaterLevelAtPosition(position, checkWaterBodyEntities: true);

            IsWater = Height < WaterLevel;
        }
    }
}
