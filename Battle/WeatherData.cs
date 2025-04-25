using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class WeatherData
    {
        public float TimeOfDay { get; set; }
        public float Rain { get; set; }
        public float Fog { get; set; }
        public float Temperature { get; set; }
        public bool IsNight { get; set; }

        public WeatherData(Scene scene)
        {
            if (scene == null) return;

            // Get time of day from scene
            TimeOfDay = scene.TimeOfDay;
            IsNight = TimeOfDay < 6f || TimeOfDay > 18f;

            // Get weather conditions
            Rain = scene.GetRainDensity();
            Fog = scene.GetFogDensity();
            Temperature = scene.GetTemperature();
        }

        public WeatherData(BattleSnapshot snapshot)
        {
            if (snapshot == null) return;

            // Copy weather data from snapshot if available
            if (snapshot.Weather != null)
            {
                TimeOfDay = snapshot.Weather.TimeOfDay;
                Rain = snapshot.Weather.Rain;
                Fog = snapshot.Weather.Fog;
                Temperature = snapshot.Weather.Temperature;
                IsNight = TimeOfDay < 6f || TimeOfDay > 18f;
            }
        }
    }
} 