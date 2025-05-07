using TaleWorlds.Library;
using TaleWorlds.Engine;

namespace HannibalAI.Battle
{
    public class WeatherData
    {
        public bool IsRaining { get; set; }
        public bool IsFoggy { get; set; }
        public float Temperature { get; set; }

        public WeatherData(Scene scene)
        {
            if (scene == null)
                return;

            // These methods don't exist directly anymore; you'll simulate weather manually
            var atmosphere = scene.GetAtmosphere();
            if (atmosphere != null)
            {
                IsRaining = atmosphere.RainDensity > 0.5f;
                IsFoggy = atmosphere.FogDensity > 0.5f;
                Temperature = atmosphere.Temperature;
            }
            else
            {
                IsRaining = false;
                IsFoggy = false;
                Temperature = 20.0f; // default temp
            }
        }
    }
}
