using System;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.UI
{
    /// <summary>
    /// Mission behavior to handle settings UI activation
    /// </summary>
    public class SettingsBehavior : MissionBehavior
    {
        private ModSettingsScreen _settingsScreen;
        private bool _isSettingsOpen;
        private bool _isInitialized;
        private bool _hasShownHelpMessage;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public SettingsBehavior()
        {
            _isSettingsOpen = false;
            _isInitialized = false;
            _hasShownHelpMessage = false;
            Logger.Instance.Info("HannibalAI SettingsBehavior created");
        }
        
        public override void OnMissionTick(float dt)
        {
            // Show the help message on first tick if enabled
            if (!_hasShownHelpMessage && !_isInitialized)
            {
                _isInitialized = true;
                _hasShownHelpMessage = true;
                
                Logger.Instance.Info("HannibalAI SettingsBehavior initialized");
                
                // Display initial help message about INSERT key
                if (ModConfig.Instance.ShowHelpMessages)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage("HannibalAI: Press INSERT key to open tactical settings", 
                        Color.FromUint(0x00CCFF)));
                }
                
                // Log settings to help debug during development
                if (ModConfig.Instance.Debug)
                {
                    LogCurrentSettings();
                }
            }
            
            // Check for key press to open settings
            if (Input.IsKeyPressed(InputKey.Insert))
            {
                Logger.Instance.Info("Settings key pressed - attempting to toggle settings screen");
                ToggleSettings();
            }
        }

        private void LogCurrentSettings()
        {
            var config = ModConfig.Instance;
            string settingsLog = "HannibalAI Mod Settings:\n" +
                                 $"- AI Controls Enemies: {config.AIControlsEnemies}\n" +
                                 $"- Use Commander Memory: {config.UseCommanderMemory}\n" + 
                                 $"- Show Help Messages: {config.ShowHelpMessages}\n" +
                                 $"- Debug Mode: {config.Debug}\n" +
                                 $"- Aggressiveness: {config.Aggressiveness}%";
            
            Logger.Instance.Info(settingsLog);
            
            // Display friendly message for debugging
            if (config.Debug)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage("HannibalAI debug mode active - detailed logging enabled", 
                    Color.FromUint(0xFFFF00)));
            }
        }

        private void ToggleSettings()
        {
            if (!_isSettingsOpen)
            {
                OpenSettings();
            }
            else
            {
                CloseSettings();
            }
        }

        private void OpenSettings()
        {
            try
            {
                if (_settingsScreen == null)
                {
                    Logger.Instance.Info("Creating settings screen");
                    
                    // Create a layer for the UI with a high priority to make it appear on top
                    var layer = new GauntletWrapper(1000);
                    
                    // Create the settings screen with the layer
                    _settingsScreen = new ModSettingsScreen(layer);
                    _settingsScreen.Initialize();
                    
                    // Mark settings as open
                    _isSettingsOpen = true;

                    // Show confirmation message
                    InformationManager.DisplayMessage(
                        new InformationMessage("HannibalAI Settings Opened (Press Insert to close)", 
                        Color.FromUint(0x00FF00)));
                    
                    Logger.Instance.Info("Settings screen created successfully");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Failed to open settings: {ex.Message}\n{ex.StackTrace}");
                InformationManager.DisplayMessage(
                    new InformationMessage($"Failed to open settings: {ex.Message}", 
                    Color.FromUint(0xFF0000)));
            }
        }

        private void CloseSettings()
        {
            if (_settingsScreen != null)
            {
                _settingsScreen.OnFinalize();
                _settingsScreen = null;
                _isSettingsOpen = false;
                
                // Show confirmation message
                InformationManager.DisplayMessage(
                    new InformationMessage("HannibalAI Settings closed", 
                    Color.FromUint(0x00FF00)));
            }
        }
        
        public override void OnRemoveBehavior()
        {
            CloseSettings();
            base.OnRemoveBehavior();
        }
    }
}