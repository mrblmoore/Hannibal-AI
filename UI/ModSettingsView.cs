using System;
using System.ComponentModel;
using HannibalAI.Config;
using HannibalAI.UI.Components;
using TaleWorlds.Library;

namespace HannibalAI.UI
{
    public class ModSettingsView : ViewModel
    {
        private ModConfig _config;
        private NumericBox _memoryDurationBox;
        private NumericBox _memoryDecayBox;

        public ModSettingsView(ModConfig config)
        {
            _config = config;
            _memoryDurationBox = new NumericBox(0f, 60f, config.CommanderMemoryDuration);
            _memoryDecayBox = new NumericBox(0f, 1f, config.CommanderMemoryDecayRate);

            _memoryDurationBox.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(NumericBox.Value))
                {
                    _config.CommanderMemoryDuration = _memoryDurationBox.Value;
                    _config.SaveConfig();
                }
            };

            _memoryDecayBox.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(NumericBox.Value))
                {
                    _config.CommanderMemoryDecayRate = _memoryDecayBox.Value;
                    _config.SaveConfig();
                }
            };
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
            get => _memoryDecayBox.Value;
            set => _memoryDecayBox.Value = value;
        }
    }
} 