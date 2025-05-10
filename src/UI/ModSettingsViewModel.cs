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
        private bool _showHelpMessages;
        private bool _debug;
        private bool _verboseLogging;
        private bool _preferHighGround;
        private bool _preferRangedFormations;
        
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
            _showHelpMessages = _config.ShowHelpMessages;
            _debug = _config.Debug;
            _verboseLogging = _config.VerboseLogging;
            _preferHighGround = _config.PreferHighGround;
            _preferRangedFormations = _config.PreferRangedFormations;
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
        
        // Show Help Messages property
        [DataSourceProperty]
        public bool ShowHelpMessages
        {
            get => _showHelpMessages;
            set
            {
                if (_showHelpMessages != value)
                {
                    _showHelpMessages = value;
                    _config.ShowHelpMessages = value;
                    OnPropertyChanged(nameof(ShowHelpMessages));
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
        
        // VerboseLogging property
        [DataSourceProperty]
        public bool VerboseLogging
        {
            get => _verboseLogging;
            set
            {
                if (_verboseLogging != value)
                {
                    _verboseLogging = value;
                    _config.VerboseLogging = value;
                    OnPropertyChanged(nameof(VerboseLogging));
                }
            }
        }
        
        // PreferHighGround property
        [DataSourceProperty]
        public bool PreferHighGround
        {
            get => _preferHighGround;
            set
            {
                if (_preferHighGround != value)
                {
                    _preferHighGround = value;
                    _config.PreferHighGround = value;
                    OnPropertyChanged(nameof(PreferHighGround));
                }
            }
        }
        
        // PreferRangedFormations property
        [DataSourceProperty]
        public bool PreferRangedFormations
        {
            get => _preferRangedFormations;
            set
            {
                if (_preferRangedFormations != value)
                {
                    _preferRangedFormations = value;
                    _config.PreferRangedFormations = value;
                    OnPropertyChanged(nameof(PreferRangedFormations));
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
            Logger.Instance.Info("HannibalAI settings saved");
            
            // Hide UI
            IsVisible = false;
            
            // Call close callback
            _onClose?.Invoke();
        }
        
        // Additional method that matches the XML UI button command
        public void ExecuteCloseSettings()
        {
            ExecuteClose();
        }
        
        // Reset to default settings
        public void ExecuteResetDefaults()
        {
            // Reset to defaults
            AIControlsEnemies = false;
            UseCommanderMemory = true;
            ShowHelpMessages = true;
            Debug = false;
            VerboseLogging = false;
            PreferHighGround = true;
            PreferRangedFormations = true;
            Aggressiveness = 50;
            
            // Update text representation
            OnPropertyChanged(nameof(AggressivenessText));
            
            // Display confirmation
            Logger.Instance.Info("HannibalAI settings reset to defaults");
            InformationManager.DisplayMessage(new InformationMessage(
                "HannibalAI settings reset to defaults", Color.FromUint(0xFFAA00)));
        }
        
        // Update the text representation when aggressiveness changes
        public void OnAggressivenessChange()
        {
            OnPropertyChanged(nameof(AggressivenessText));
        }
        
        // Refresh all values from config (override base method)
        public override void RefreshValues()
        {
            if (_config != null)
            {
                // Assign without triggering property changed
                _aiControlsEnemies = _config.AIControlsEnemies;
                _useCommanderMemory = _config.UseCommanderMemory;
                _showHelpMessages = _config.ShowHelpMessages;
                _debug = _config.Debug;
                _verboseLogging = _config.VerboseLogging;
                _preferHighGround = _config.PreferHighGround;
                _preferRangedFormations = _config.PreferRangedFormations;
                _aggressiveness = _config.Aggressiveness;
                
                // Notify UI about all changes at once
                OnPropertyChanged(nameof(AIControlsEnemies));
                OnPropertyChanged(nameof(UseCommanderMemory));
                OnPropertyChanged(nameof(ShowHelpMessages));
                OnPropertyChanged(nameof(Debug));
                OnPropertyChanged(nameof(VerboseLogging));
                OnPropertyChanged(nameof(PreferHighGround));
                OnPropertyChanged(nameof(PreferRangedFormations));
                OnPropertyChanged(nameof(Aggressiveness));
                OnPropertyChanged(nameof(AggressivenessText));
                
                Logger.Instance.Info("[HannibalAI] UI values refreshed from config");
            }
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