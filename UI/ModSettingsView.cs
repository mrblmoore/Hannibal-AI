using System;
using System.ComponentModel;
using HannibalAI.Config;
using TaleWorlds.Library;

namespace HannibalAI.UI
{
    public class ModSettingsView : ViewModel
    {
        private ModConfig _config;
        private float _memoryDuration;
        private float _memoryDecay;

        public ModSettingsView(ModConfig config)
        {
            _config = config;
            _memoryDuration = config.CommanderMemoryDuration;
            _memoryDecay = config.CommanderMemoryDecayRate;
        }

        [DataSourceProperty]
        public float MemoryDuration
        {
            get => _memoryDuration;
            set
            {
                if (_memoryDuration != value)
                {
                    _memoryDuration = value;
                    _config.CommanderMemoryDuration = value;
                    _config.SaveConfig();
                    OnPropertyChanged(nameof(MemoryDuration));
                }
            }
        }

        [DataSourceProperty]
        public float MemoryDecay
        {
            get => _memoryDecay;
            set
            {
                if (_memoryDecay != value)
                {
                    _memoryDecay = value;
                    _config.CommanderMemoryDecayRate = value;
                    _config.SaveConfig();
                    OnPropertyChanged(nameof(MemoryDecay));
                }
            }
        }
    }
} 