using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.UI
{
    /// <summary>
    /// Settings screen for HannibalAI mod
    /// </summary>
    public class ModSettingsScreen
    {
        private ModSettingsViewModel _dataSource;
        
        public ModSettingsScreen()
        {
            // Create the view model with config
            _dataSource = new ModSettingsViewModel(ModConfig.Instance, OnClose);
        }
        
        public void Initialize()
        {
            // Display a message when settings are opened
            InformationManager.DisplayMessage(new InformationMessage("HannibalAI settings opened"));
        }
        
        public void CleanUp()
        {
            _dataSource.OnFinalize();
            _dataSource = null;
        }
        
        // Close callback
        private void OnClose()
        {
            // Clean up
            CleanUp();
        }
    }
}