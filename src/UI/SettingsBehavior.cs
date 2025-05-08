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
    public override void OnMissionTick(float dt)
    {
        if (Input.IsKeyPressed(InputKey.F5))
        {
            var layer = new GauntletLayer(1000);
            var vm = new ModSettingsViewModel();
            layer.LoadMovie("HannibalAI_Settings", vm);
            MissionScreen.AddLayer(layer);
            InformationManager.DisplayMessage(new InformationMessage("HannibalAI Settings Opened"));
        }
    }
}
    {
        private ModSettingsScreen _settingsScreen;
        private bool _isSettingsOpen;
        
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        
        public SettingsBehavior()
        {
            _isSettingsOpen = false;
        }
        
        public override void OnMissionTick(float dt)
        {
            // Check for key press to open settings
            if (Input.IsKeyPressed(InputKey.F5))
            {
                ToggleSettings();
            }
        }
        
        public void ToggleSettings()
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
            // Create and initialize settings screen
            _settingsScreen = new ModSettingsScreen();
            _settingsScreen.Initialize();
            _isSettingsOpen = true;
            
            // Show debug message
            if (ModConfig.Instance.Debug)
            {
                Logger.Instance.Info("Press F5 to close HannibalAI settings");
            }
        }
        
        private void CloseSettings()
        {
            // Clean up settings screen
            if (_settingsScreen != null)
            {
                _settingsScreen.CleanUp();
                _settingsScreen = null;
            }
            
            _isSettingsOpen = false;
        }
        
        public override void OnRemoveBehavior()
        {
            CloseSettings();
            base.OnRemoveBehavior();
        }
    }
}