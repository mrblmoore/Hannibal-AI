using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;

namespace HannibalAI.Battle
{
    public class WeatherData
    {
        public float RainIntensity { get; set; }
        public float FogDensity { get; set; }
        public float WindSpeed { get; set; }
        public float Temperature { get; set; }
        public bool IsNight { get; set; }

        public WeatherData()
        {
            RainIntensity = 0f;
            FogDensity = 0f;
            WindSpeed = 0f;
            Temperature = 20f;
            IsNight = false;
        }
    }
} 