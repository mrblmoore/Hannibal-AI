using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using HannibalAI.Config;

namespace HannibalAI.UI
{
    public class ModSettingsView : ViewModel
    {
        private ModConfig _config;

        public ModSettingsView(ModConfig config)
        {
            _config = config;
        }

        [DataSourceProperty]
        public float CommanderMemoryDuration
        {
            get => _config.CommanderMemoryDuration;
            set
            {
                if (_config.CommanderMemoryDuration != value)
                {
                    _config.CommanderMemoryDuration = value;
                    OnPropertyChanged(nameof(CommanderMemoryDuration));
                }
            }
        }

        [DataSourceProperty]
        public float CommanderMemoryDecayRate
        {
            get => _config.CommanderMemoryDecayRate;
            set
            {
                if (_config.CommanderMemoryDecayRate != value)
                {
                    _config.CommanderMemoryDecayRate = value;
                    OnPropertyChanged(nameof(CommanderMemoryDecayRate));
                }
            }
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            
            // Notify UI of any changes
            OnPropertyChanged(nameof(CommanderMemoryDuration));
            OnPropertyChanged(nameof(CommanderMemoryDecayRate));
        }
    }
} 