using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.UI
{
    /// <summary>
    /// Simplified layer for UI (compatibility without requiring Bannerlord DLLs)
    /// </summary>
    public class GauntletLayer
    {
        public GauntletLayer(int layer) 
        {
            // Simple constructor
            Logger.Instance.Info($"Creating GauntletLayer with priority {layer}");
        }
        
        public InputRestrictions InputRestrictions { get; } = new InputRestrictions();
        
        public object LoadMovie(string name, object dataSource)
        {
            // Simplified implementation that just logs the movie loading
            Logger.Instance.Info($"Loading UI movie: {name}");
            InformationManager.DisplayMessage(
                new InformationMessage($"HannibalAI: Loading UI for {name}", Color.FromUint(0x00FF00)));
            return new object(); // Return a generic object as we don't need the actual movie
        }
    }
    
    /// <summary>
    /// Simple input restrictions class
    /// </summary>
    public class InputRestrictions
    {
        public void ResetInputRestrictions()
        {
            // Simple implementation
            Logger.Instance.Info("Resetting input restrictions");
        }
        
        public void SetInputRestrictions(bool useKeyboardEvents, object usageMask)
        {
            // Simple implementation
            Logger.Instance.Info($"Setting input restrictions: useKeyboardEvents={useKeyboardEvents}");
        }
    }
    
    /// <summary>
    /// Settings screen for HannibalAI mod
    /// </summary>
    public class ModSettingsScreen
    {
        private ModSettingsViewModel _dataSource;
        private GauntletLayer _layer;
        private ModSettingsView _view;
        
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
            
            if (_layer != null)
            {
                try 
                {
                    // Create a proper view using the ModSettingsView class
                    _view = new ModSettingsView(_dataSource);
                    if (_view.Initialize(_layer))
                    {
                        Logger.Instance.Info("Settings view initialized successfully");
                    }
                    else
                    {
                        Logger.Instance.Error("Failed to initialize settings view");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error($"Error initializing settings UI: {ex.Message}");
                }
            }
            else
            {
                Logger.Instance.Warning("No layer provided for UI, using fallback display");
            }
            
            // Make sure UI is visible
            _dataSource.IsVisible = true;
            
            // Display settings in a simple way as fallback
            DisplayCurrentSettings();
        }
        
        private void DisplayCurrentSettings()
        {
            // For now, just show the current settings in the log/message system
            var config = ModConfig.Instance;
            string settings = $"HannibalAI Settings:\n" +
                              $"- AI Controls Enemies: {(config.AIControlsEnemies ? "ENABLED" : "disabled")}\n" +
                              $"- Use Commander Memory: {(config.UseCommanderMemory ? "ENABLED" : "disabled")}\n" +
                              $"- Show Help Messages: {(config.ShowHelpMessages ? "ENABLED" : "disabled")}\n" +
                              $"- Debug Mode: {(config.Debug ? "ENABLED" : "disabled")}\n" +
                              $"- Aggressiveness: {config.Aggressiveness}%";
            
            Logger.Instance.Info(settings);
            
            // Show abbreviated settings as an immediate message
            InformationManager.DisplayMessage(new InformationMessage(
                $"HannibalAI: Enemy AI Control {(config.AIControlsEnemies ? "ON" : "OFF")} | " +
                $"Commander Memory {(config.UseCommanderMemory ? "ON" : "OFF")} | " +
                $"Aggression {config.Aggressiveness}%"));
        }
        
        public void CleanUp()
        {
            if (_dataSource != null)
            {
                _dataSource.OnFinalize();
                _dataSource = null;
            }
            
            if (_view != null)
            {
                _view.OnFinalize();
                _view = null;
            }
        }
        
        public void OnFinalize()
        {
            // Handle layer cleanup if using GauntletUI
            if (_layer != null)
            {
                // Reset input restrictions
                _layer.InputRestrictions.ResetInputRestrictions();
                
                // Log that we're removing the layer
                Logger.Instance.Info("ModSettingsScreen: Removing UI layer from mission screen");
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