using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;

namespace HannibalAI.Battle
{
    public class WeatherData
    {
        public float TimeOfDay { get; set; }
        public float Rain { get; set; }
        public float Fog { get; set; }

        public WeatherData(Mission mission)
        {
            if (mission == null || mission.Scene == null) return;

            TimeOfDay = Mission.Current.MissionTime;
            Rain = mission.Scene.GetRainDensity();
            Fog = 0f; // Fog is not directly accessible in current API
        }
    }
} 