using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.MountAndBlade.View;
using HannibalAI.Services;

namespace HannibalAI.UI
{
    [ViewScript("ModSettingsView")]
    public class ModSettingsView : View
    {
        private ModConfig _modConfig;

        public ModSettingsView(GauntletLayer layer) : base(layer)
        {
            _modConfig = new ModConfig();

            // Example of binding data (optional for expansion)
            var dataSource = new ModSettingsDataSource(_modConfig);
            layer.LoadMovie("ModSettings", dataSource);
        }

        // This could be extended later for UI interaction
    }

    public class ModSettingsDataSource : ViewModel
    {
        private readonly ModConfig _config;

        public ModSettingsDataSource(ModConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public int CommanderMemoryDuration
        {
            get => _config.CommanderMemoryDuration;
            set => _config.CommanderMemoryDuration = value;
        }

        public float CommanderMemoryDecayRate
        {
            get => _config.CommanderMemoryDecayRate;
            set => _config.CommanderMemoryDecayRate = value;
        }

        public int CommanderMemoryMaxValue
        {
            get => _config.CommanderMemoryMaxValue;
            set => _config.CommanderMemoryMaxValue = value;
        }

        public int CommanderMemoryMinValue
        {
            get => _config.CommanderMemoryMinValue;
            set => _config.CommanderMemoryMinValue = value;
        }

        public bool Debug
        {
            get => _config.Debug;
            set => _config.Debug = value;
        }
    }
}
