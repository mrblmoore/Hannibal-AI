using System;
using System.ComponentModel;
using HannibalAI.Config;
using HannibalAI.UI.Components;
using TaleWorlds.Library;

namespace HannibalAI.UI
{
    public class ModSettingsView : ViewModel
    {
        private readonly ModConfig _config;
        private readonly NumericBox _memoryDurationBox;
        private readonly NumericBox _memoryDecayRateBox;

        public ModSettingsView()
        {
            _config = ModConfig.Instance;
            _memoryDurationBox = new NumericBox(_config.CommanderMemoryDuration, 0f, 3600f, 30f);
            _memoryDecayRateBox = new NumericBox(_config.CommanderMemoryDecayRate, 0f, 1f, 0.1f);
        }

        public void OnMemoryDurationChanged()
        {
            if (_config != null)
            {
                _config.CommanderMemoryDuration = _memoryDurationBox.Value;
                _config.SaveConfig();
            }
        }

        public void OnMemoryDecayRateChanged()
        {
            if (_config != null)
            {
                _config.CommanderMemoryDecayRate = _memoryDecayRateBox.Value;
                _config.SaveConfig();
            }
        }

        [DataSourceProperty]
        public float MemoryDuration
        {
            get => _memoryDurationBox.Value;
            set => _memoryDurationBox.Value = value;
        }

        [DataSourceProperty]
        public float MemoryDecay
        {
            get => _memoryDecayRateBox.Value;
            set => _memoryDecayRateBox.Value = value;
        }
    }
} 