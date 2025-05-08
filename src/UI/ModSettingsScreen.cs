using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine.GauntletUI;

namespace HannibalAI.UI
{
    /// <summary>
    /// Settings screen for HannibalAI mod
    /// </summary>
    public class ModSettingsScreen
    {
        private ModSettingsViewModel _dataSource;
        private GauntletLayer _layer;
        
        // Default constructor for simple implementation
        public ModSettingsScreen()
        {
            // Create the view model with config
            _dataSource = new ModSettingsViewModel(ModConfig.Instance, OnClose);
            _layer = null;
        }
        
        // Constructor with GauntletLayer for UI implementation
        public ModSettingsScreen(GauntletLayer layer)
        {
            _layer = layer;
            _dataSource = new ModSettingsViewModel(ModConfig.Instance, OnClose);
        }
        
        public void Initialize()
        {
            // Display a message when settings are opened
            Logger.Instance.Info("HannibalAI settings opened");
            
            // Make sure UI is visible
            _dataSource.IsVisible = true;
            
            // Display settings in a simple way
            DisplayCurrentSettings();
        }
        
        private void DisplayCurrentSettings()
        {
            // For now, just show the current settings in the log/message system
            var config = ModConfig.Instance;
            string settings = $"HannibalAI Settings:\n" +
                              $"- AI Controls Enemies: {(config.AIControlsEnemies ? "ENABLED" : "disabled")}\n" +
                              $"- Use Commander Memory: {(config.UseCommanderMemory ? "ENABLED" : "disabled")}\n" +
                              $"- Debug Mode: {(config.Debug ? "ENABLED" : "disabled")}\n" +
                              $"- Aggressiveness: {config.Aggressiveness}%";
            
            Logger.Instance.Info(settings);
            
            // Show abbreviated settings as an immediate message
            InformationManager.DisplayMessage(new InformationMessage(
                $"HannibalAI: Enemy AI Control {(config.AIControlsEnemies ? "ON" : "OFF")} | " +
                $"Commander Memory {(config.UseCommanderMemory ? "ON" : "OFF")} | " +
                $"Aggression {config.Aggressiveness}%"));
            
            // Display an informational message to the player
            InformationManager.DisplayMessage(new InformationMessage("HannibalAI settings opened. Use console for detailed view."));
        }
        
        public void CleanUp()
        {
            if (_dataSource != null)
            {
                _dataSource.OnFinalize();
                _dataSource = null;
            }
        }
        
        public void OnFinalize()
        {
            // Handle layer cleanup if using GauntletUI
            if (_layer != null)
            {
                _layer.InputRestrictions.ResetInputRestrictions();
                MissionScreen.RemoveLayer(_layer);
            }
            
            // Clean up data source
            CleanUp();
        }
        
        // Close callback
        private void OnClose()
        {
            // Save settings before closing
            ModConfig.Instance.SaveSettings();
            
            // Clean up
            OnFinalize();
        }
    }
}