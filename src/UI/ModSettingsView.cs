using System;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace HannibalAI.UI
{
    /// <summary>
    /// Minimal implementation of a View class to display settings
    /// Uses only standard Bannerlord namespaces that we know are available
    /// </summary>
    public class ModSettingsView : ViewModel
    {
        private ModSettingsViewModel _dataSource;
        
        public ModSettingsView(ModSettingsViewModel dataSource)
        {
            _dataSource = dataSource;
            Logger.Instance.Info("ModSettingsView constructor called");
        }
        
        public bool Initialize(GauntletLayer gauntletLayer)
        {
            try
            {
                // Log the initialization attempt
                Logger.Instance.Info("Initializing ModSettingsView");
                
                // Try to load the Gauntlet UI definition
                Logger.Instance.Info("Attempting to load HannibalAI_Settings prefab");
                
                try 
                {
                    // Create a movie using our prefab and view model
                    var movie = gauntletLayer.LoadMovie("HannibalAI_Settings", _dataSource);
                    if (movie != null)
                    {
                        Logger.Instance.Info("Successfully loaded HannibalAI_Settings prefab");
                        return true;
                    }
                    else
                    {
                        Logger.Instance.Warning("Failed to load HannibalAI_Settings prefab, using fallback");
                    }
                }
                catch (Exception uiEx)
                {
                    // Log the failure but continue with fallback
                    Logger.Instance.Error($"Failed to load UI definition: {uiEx.Message}");
                }
                
                // Display settings as a fallback if we couldn't load the UI
                DisplaySettings();
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error initializing ModSettingsView: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        private void DisplaySettings()
        {
            // Display current settings using the InformationManager
            var settings = new[]
            {
                $"AI Controls Enemies: {(_dataSource.AIControlsEnemies ? "ON" : "off")}",
                $"Use Commander Memory: {(_dataSource.UseCommanderMemory ? "ON" : "off")}",
                $"Debug Mode: {(_dataSource.Debug ? "ON" : "off")}",
                $"Aggressiveness: {_dataSource.Aggressiveness}%"
            };
            
            // Show each setting as a separate message
            foreach (var setting in settings)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"HannibalAI Setting: {setting}", 
                    Color.FromUint(0x00CCFF)));
            }
            
            // Final instruction
            InformationManager.DisplayMessage(new InformationMessage(
                "Press INSERT again to close settings", 
                Color.FromUint(0x00FF00)));
        }
        
        public override void OnFinalize()
        {
            _dataSource = null;
            base.OnFinalize();
        }
    }
}