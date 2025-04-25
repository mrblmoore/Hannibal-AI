using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class TerrainData
    {
        public float Height { get; set; }
        public float AverageHeight { get; set; }
        public float WaterLevel { get; set; }
        public bool IsWater { get; set; }
        public bool HasWater { get; set; }
        public bool HasForest { get; set; }
        public bool HasHills { get; set; }

        public TerrainData(Scene scene)
        {
            if (scene == null) return;

            AverageHeight = GetAverageHeight(scene);
            Height = AverageHeight;
            WaterLevel = scene.GetWaterLevel();
            IsWater = false; // Scene.IsWaterAtPosition is not available in current API
            HasWater = WaterLevel > float.MinValue;
            HasForest = EstimateHasForest(scene);
            HasHills = EstimateHasHills(scene);
        }

        private float GetAverageHeight(Scene scene)
        {
            if (scene == null) return 0f;

            float totalHeight = 0f;
            int sampleCount = 0;
            const int gridSize = 10;
            const float step = 100f;

            for (int x = -gridSize; x <= gridSize; x++)
            {
                for (int y = -gridSize; y <= gridSize; y++)
                {
                    var pos = new Vec2(x * step, y * step);
                    float height = 0f;
                    if (scene.GetHeightAtPoint(pos, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref height))
                    {
                        totalHeight += height;
                        sampleCount++;
                    }
                }
            }

            return sampleCount > 0 ? totalHeight / sampleCount : 0f;
        }

        private bool EstimateHasForest(Scene scene)
        {
            if (scene == null) return false;

            // Since we can't directly check for trees, we'll use a simpler approach
            // This is a placeholder - you may want to implement a more accurate method
            return false;
        }

        private bool EstimateHasHills(Scene scene)
        {
            if (scene == null) return false;

            float totalHeight = 0f;
            float maxHeight = float.MinValue;
            float minHeight = float.MaxValue;
            int sampleCount = 0;
            const int gridSize = 10;
            const float step = 100f;

            for (int x = -gridSize; x <= gridSize; x++)
            {
                for (int y = -gridSize; y <= gridSize; y++)
                {
                    var pos = new Vec2(x * step, y * step);
                    float height = 0f;
                    if (scene.GetHeightAtPoint(pos, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref height))
                    {
                        totalHeight += height;
                        maxHeight = Math.Max(maxHeight, height);
                        minHeight = Math.Min(minHeight, height);
                        sampleCount++;
                    }
                }
            }

            if (sampleCount == 0) return false;

            float averageHeight = totalHeight / sampleCount;
            float heightRange = maxHeight - minHeight;

            return heightRange > 5f;
        }
    }
} 