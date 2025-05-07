using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace HannibalAI
{
    public class SubModule : MBSubModuleBase
    {
        private AIService _aiService;
        private ModConfig _config;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                // Initialize mod configuration
                _config = new ModConfig();
                _config.LoadSettings();

                // Log mod initialization
                InformationManager.DisplayMessage(new InformationMessage("HannibalAI is loading..."));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error initializing HannibalAI: {ex.Message}"));
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            try
            {
                // Initialize the AI service
                _aiService = new AIService(_config);

                // Print success message
                InformationManager.DisplayMessage(new InformationMessage("HannibalAI has been initialized successfully!"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error initializing HannibalAI AI service: {ex.Message}"));
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            try
            {
                // Only add our controller to battle missions
                if (mission.IsBattleMission)
                {
                    mission.AddMissionBehavior(new BattleController(_aiService));
                    
                    if (_config.VerboseLogging)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("HannibalAI is active in this battle"));
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error adding HannibalAI to mission: {ex.Message}"));
            }
        }
    }
}
