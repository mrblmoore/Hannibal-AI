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

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public SettingsBehavior()
        {
            _isSettingsOpen = false;
        }

        public override void OnMissionTick(float dt)
        {
            // Check for key press to open settings
            if (Input.IsKeyPressed(InputKey.Insert) || 
                (Input.IsKeyPressed(InputKey.Insert) && Input.IsKeyDown(InputKey.LeftAlt)))
            {
                Logger.Instance.Info("Settings key pressed - attempting to toggle settings screen");
                ToggleSettings();
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
                    var layer = new GauntletLayer(1000);
                    _settingsScreen = new ModSettingsScreen(layer);
                    _settingsScreen.Initialize();
                    _isSettingsOpen = true;

                    InformationManager.DisplayMessage(
                        new InformationMessage("HannibalAI Settings Opened (Press Insert to close)", Color.FromUint(0x00FF00)));
                    
                    Logger.Instance.Info("Settings screen created successfully");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Failed to open settings: {ex.Message}");
                InformationManager.DisplayMessage(
                    new InformationMessage($"Failed to open settings: {ex.Message}", Color.FromUint(0xFF0000)));
            }
        }

        private void CloseSettings()
        {
            if (_settingsScreen != null)
            {
                _settingsScreen.OnFinalize();
                _settingsScreen = null;
                _isSettingsOpen = false;
            }
        }
        public override void OnRemoveBehavior()
        {
            CloseSettings();
            base.OnRemoveBehavior();
        }
    }
}