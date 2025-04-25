using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class WeatherData
    {
        public float RainDensity { get; private set; }
        public float SnowDensity { get; private set; }
        public float FogDensity { get; private set; }
        public float Temperature { get; private set; }

        public WeatherData(Scene scene)
        {
            if (scene == null) return;

            // Get weather state from scene
            var weatherState = scene.GetWeatherState();
            if (weatherState != null)
            {
                RainDensity = weatherState.RainDensity;
                SnowDensity = weatherState.SnowDensity;
                FogDensity = weatherState.FogDensity;
                Temperature = 20f; // Default temperature since GetTemperature() is not available
            }
            else
            {
                // Default values if weather state is not available
                RainDensity = 0f;
                SnowDensity = 0f;
                FogDensity = 0f;
                Temperature = 20f;
            }
        }

        public WeatherData(float rainDensity, float snowDensity, float fogDensity, float temperature)
        {
            RainDensity = rainDensity;
            SnowDensity = snowDensity;
            FogDensity = fogDensity;
            Temperature = temperature;
        }

        public WeatherData(BattleSnapshot snapshot)
        {
            if (snapshot == null) return;

            // Copy weather data from snapshot if available
            if (snapshot.Weather != null)
            {
                RainDensity = snapshot.Weather.RainDensity;
                SnowDensity = snapshot.Weather.SnowDensity;
                FogDensity = snapshot.Weather.FogDensity;
                Temperature = snapshot.Weather.Temperature;
            }
        }
    }
} 