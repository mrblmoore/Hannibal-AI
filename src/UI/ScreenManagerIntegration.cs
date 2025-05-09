using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.UI
{
    /// <summary>
    /// Provides integration with Bannerlord's screen manager system to register UI screens
    /// </summary>
    public static class ScreenManagerIntegration
    {
        private static bool _screenRegistered = false;
        private static Type _screenManagerType = null;
        private static object _screenManager = null;
        
        /// <summary>
        /// Initialize the screen manager integration
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                // Try to get the screen manager type and instance
                if (_screenManagerType == null)
                {
                    // Look for TaleWorlds.Engine.Screens.ScreenManager
                    _screenManagerType = Type.GetType("TaleWorlds.Engine.Screens.ScreenManager, TaleWorlds.Engine");
                    
                    if (_screenManagerType == null)
                    {
                        Logger.Instance.Warning("Could not find ScreenManager type, UI integration may be limited");
                        return false;
                    }
                    
                    // Get the Instance property
                    var instanceProperty = _screenManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceProperty != null)
                    {
                        _screenManager = instanceProperty.GetValue(null);
                        Logger.Instance.Info("Successfully got ScreenManager instance");
                    }
                    else
                    {
                        Logger.Instance.Warning("Could not find ScreenManager.Instance property");
                        return false;
                    }
                }
                
                return _screenManager != null;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error initializing ScreenManager integration: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Register the mod settings UI with the screen manager
        /// </summary>
        public static bool RegisterModSettingsScreen()
        {
            if (_screenRegistered)
            {
                return true;
            }
            
            try
            {
                if (!Initialize())
                {
                    Logger.Instance.Error("Could not initialize ScreenManager integration");
                    return false;
                }
                
                // Try to find PushLayer, PushScreen, or AddLayer method
                // Different versions of Bannerlord might use different methods
                MethodInfo pushMethod = null;
                
                // Try PushLayer first as it's most common
                pushMethod = _screenManagerType.GetMethod("PushLayer", 
                    BindingFlags.Public | BindingFlags.Instance);
                
                if (pushMethod == null)
                {
                    // Try PushScreen next
                    pushMethod = _screenManagerType.GetMethod("PushScreen",
                        BindingFlags.Public | BindingFlags.Instance);
                }
                
                if (pushMethod == null)
                {
                    // Try AddLayer as a last resort
                    pushMethod = _screenManagerType.GetMethod("AddLayer",
                        BindingFlags.Public | BindingFlags.Instance);
                }
                
                if (pushMethod != null)
                {
                    // We found a method to push screens/layers
                    // Mark as registered for now (actual registration happens during gameplay)
                    _screenRegistered = true;
                    Logger.Instance.Info("Screen registration method found");
                    
                    // Display a confirmation message for the user
                    InformationManager.DisplayMessage(new InformationMessage(
                        "HannibalAI UI integration enabled - Press INSERT to access settings",
                        Color.FromUint(0x00FF00)));
                    
                    return true;
                }
                else
                {
                    Logger.Instance.Warning("Could not find method to push screens/layers");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error registering mod settings screen: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Show a message telling the user how to access the settings
        /// </summary>
        public static void ShowSettingsHelpMessage()
        {
            try
            {
                if (ModConfig.Instance.ShowHelpMessages)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "HannibalAI: Press INSERT to access tactical settings",
                        Color.FromUint(0x00CCFF)));
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error showing settings help message: {ex.Message}");
            }
        }
    }
}