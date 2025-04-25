using TaleWorlds.Engine;

namespace HannibalAI.Battle
{
    public class WeatherData
    {
        public float FogDensity { get; private set; }
        public float Temperature { get; private set; }

        public WeatherData(Scene scene)
        {
            if (scene == null)
            {
                FogDensity = 0f;
                Temperature = 20f; // Default temperature
                return;
            }

            // No direct API exists in 1.2.12 for Fog or Temperature.
            // Therefore, we assign reasonable defaults or use environmental lighting later if needed.
            FogDensity = 0f;  // Placeholder (Bannerlord API does not expose fog density anymore)
            Temperature = 20f; // Placeholder temperature (average)

            // Future Expansion: You can later hook into weather particles, ambient lighting, etc.
        }
    }
}
