using System;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine.GauntletUI;

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
            if (Input.IsKeyPressed(InputKey.F5))
            {
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
            if (_settingsScreen == null)
            {
                var layer = new GauntletLayer(1000);
                var vm = new ModSettingsViewModel();
                layer.LoadMovie("HannibalAI_Settings", vm);
                MissionScreen.AddLayer(layer);
                _settingsScreen = new ModSettingsScreen(layer);
                _isSettingsOpen = true;

                InformationManager.DisplayMessage(
                    new InformationMessage("HannibalAI Settings Opened", Color.FromUint(0x00FF00)));
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