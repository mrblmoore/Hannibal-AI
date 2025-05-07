using System;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace HannibalAI.UI
{
    /// <summary>
    /// ViewModel for HannibalAI mod settings
    /// </summary>
    public class ModSettingsViewModel : ViewModel
    {
        private ModConfig _config;
        private Action _onClose;
        
        // Flag properties (toggle controls)
        private bool _aiControlsEnemies;
        private bool _useCommanderMemory;
        private bool _debug;
        
        // Slider properties
        private int _aggressiveness;
        
        // UI state
        private bool _isVisible;
        
        public ModSettingsViewModel(ModConfig config, Action onClose)
        {
            _config = config;
            _onClose = onClose;
            
            // Initialize properties from config
            _aiControlsEnemies = _config.AIControlsEnemies;
            _useCommanderMemory = _config.UseCommanderMemory;
            _debug = _config.Debug;
            _aggressiveness = _config.Aggressiveness;
            
            _isVisible = true;
        }
        
        // AI Controls Enemies property
        [DataSourceProperty]
        public bool AIControlsEnemies
        {
            get => _aiControlsEnemies;
            set
            {
                if (_aiControlsEnemies != value)
                {
                    _aiControlsEnemies = value;
                    _config.AIControlsEnemies = value;
                    OnPropertyChanged(nameof(AIControlsEnemies));
                }
            }
        }
        
        // Use Commander Memory property
        [DataSourceProperty]
        public bool UseCommanderMemory
        {
            get => _useCommanderMemory;
            set
            {
                if (_useCommanderMemory != value)
                {
                    _useCommanderMemory = value;
                    _config.UseCommanderMemory = value;
                    OnPropertyChanged(nameof(UseCommanderMemory));
                }
            }
        }
        
        // Debug property
        [DataSourceProperty]
        public bool Debug
        {
            get => _debug;
            set
            {
                if (_debug != value)
                {
                    _debug = value;
                    _config.Debug = value;
                    OnPropertyChanged(nameof(Debug));
                }
            }
        }
        
        // Aggressiveness property
        [DataSourceProperty]
        public int Aggressiveness
        {
            get => _aggressiveness;
            set
            {
                if (_aggressiveness != value)
                {
                    _aggressiveness = value;
                    _config.Aggressiveness = value;
                    OnPropertyChanged(nameof(Aggressiveness));
                }
            }
        }
        
        // UI visibility property
        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChanged(nameof(IsVisible));
                }
            }
        }
        
        // Aggressiveness text representation for UI
        [DataSourceProperty]
        public string AggressivenessText => $"{_aggressiveness}%";
        
        // Close the settings screen
        public void ExecuteClose()
        {
            // Save settings
            _config.SaveSettings();
            
            // Display confirmation
            InformationManager.DisplayMessage(new InformationMessage("HannibalAI settings saved"));
            
            // Hide UI
            IsVisible = false;
            
            // Call close callback
            _onClose?.Invoke();
        }
        
        // Reset to default settings
        public void ExecuteResetDefaults()
        {
            // Reset to defaults
            AIControlsEnemies = false;
            UseCommanderMemory = true;
            Debug = false;
            Aggressiveness = 50;
            
            // Update text representation
            OnPropertyChanged(nameof(AggressivenessText));
            
            // Display confirmation
            InformationManager.DisplayMessage(new InformationMessage("HannibalAI settings reset to defaults"));
        }
        
        // Update the text representation when aggressiveness changes
        public void OnAggressivenessChange()
        {
            OnPropertyChanged(nameof(AggressivenessText));
        }
        
        // Clean up
        public override void OnFinalize()
        {
            base.OnFinalize();
            _config = null;
            _onClose = null;
        }
    }
}