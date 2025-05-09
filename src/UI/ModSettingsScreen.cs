using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.UI
{
    // We're using TaleWorlds.GauntletUI namespace which provides:
// - GauntletLayer class
// - IGauntletMovie interface
// - InputRestrictions class

/// <summary>
/// Wrapper for GauntletLayer to help with testing and compatibility
/// </summary>
public class GauntletWrapper
{
    // Static flag indicating if we're using actual Bannerlord GauntletUI
    public static bool UseActualGauntlet = true;
    
    // The actual layer (only set when using real GauntletUI)
    private object _actualLayer;
    
    // The actual input restrictions
    private object _inputRestrictions;
    
    public GauntletWrapper(int layerPriority)
    {
        try
        {
            if (UseActualGauntlet)
            {
                // Try to create an actual GauntletLayer using reflection
                Type gauntletLayerType = Type.GetType("TaleWorlds.GauntletUI.GauntletLayer, TaleWorlds.GauntletUI");
                if (gauntletLayerType != null)
                {
                    _actualLayer = Activator.CreateInstance(gauntletLayerType, new object[] { Mission.Current.Scene, layerPriority });
                    
                    // Get InputRestrictions property
                    var inputRestrictionsProperty = gauntletLayerType.GetProperty("InputRestrictions");
                    if (inputRestrictionsProperty != null)
                    {
                        _inputRestrictions = inputRestrictionsProperty.GetValue(_actualLayer);
                    }
                    
                    Logger.Instance.Info("Created actual GauntletLayer");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error($"Failed to create actual GauntletLayer: {ex.Message}");
        }
        
        // Fallback to fake implementation
        _actualLayer = null;
        _inputRestrictions = new DummyInputRestrictions();
        Logger.Instance.Warning("Using dummy GauntletLayer");
    }
    
    public object LoadMovie(string movieName, object dataSource)
    {
        try
        {
            if (_actualLayer != null)
            {
                // Try to call LoadMovie on the actual layer
                var method = _actualLayer.GetType().GetMethod("LoadMovie", new[] { typeof(string), typeof(object) });
                if (method != null)
                {
                    var result = method.Invoke(_actualLayer, new[] { movieName, dataSource });
                    Logger.Instance.Info($"Loaded movie '{movieName}' using actual GauntletLayer");
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error($"Failed to load movie: {ex.Message}");
        }
        
        // Fallback implementation
        Logger.Instance.Info($"Simulated loading of movie: {movieName}");
        InformationManager.DisplayMessage(
            new InformationMessage($"HannibalAI: Loading UI for {movieName}", Color.FromUint(0x00FF00)));
        return new object();
    }
    
    public object InputRestrictions => _inputRestrictions;
    
    public void SetInputRestrictions(bool useKeyboardEvents, object mask)
    {
        try
        {
            if (_inputRestrictions != null)
            {
                var method = _inputRestrictions.GetType().GetMethod("SetInputRestrictions", 
                    new[] { typeof(bool), typeof(object) });
                
                if (method != null)
                {
                    method.Invoke(_inputRestrictions, new[] { useKeyboardEvents, mask });
                    Logger.Instance.Info($"Set input restrictions: useKeyboardEvents={useKeyboardEvents}");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error($"Failed to set input restrictions: {ex.Message}");
        }
        
        // Fallback
        Logger.Instance.Info($"Simulated setting input restrictions: useKeyboardEvents={useKeyboardEvents}");
    }
    
    public void ResetInputRestrictions()
    {
        try
        {
            if (_inputRestrictions != null)
            {
                var method = _inputRestrictions.GetType().GetMethod("ResetInputRestrictions");
                if (method != null)
                {
                    method.Invoke(_inputRestrictions, null);
                    Logger.Instance.Info("Reset input restrictions");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.Error($"Failed to reset input restrictions: {ex.Message}");
        }
        
        // Fallback
        Logger.Instance.Info("Simulated resetting input restrictions");
    }
    
    /// <summary>
    /// Dummy implementation of input restrictions
    /// </summary>
    private class DummyInputRestrictions
    {
        public void SetInputRestrictions(bool useKeyboardEvents, object mask)
        {
            Logger.Instance.Info($"Dummy input restrictions: useKeyboardEvents={useKeyboardEvents}");
        }
        
        public void ResetInputRestrictions()
        {
            Logger.Instance.Info("Dummy input restrictions reset");
        }
    }
}
    
    /// <summary>
    /// Settings screen for HannibalAI mod
    /// </summary>
    public class ModSettingsScreen
    {
        private ModSettingsViewModel _dataSource;
        private GauntletWrapper _layer;
        private ModSettingsView _view;
        
        // Default constructor for simple implementation
        public ModSettingsScreen()
        {
            // Create the view model with config
            _dataSource = new ModSettingsViewModel(ModConfig.Instance, OnClose);
            _layer = null;
        }
        
        // Constructor with GauntletWrapper for UI implementation
        public ModSettingsScreen(GauntletWrapper layer)
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
                _layer.ResetInputRestrictions();
                
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