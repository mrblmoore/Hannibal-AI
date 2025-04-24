using System;
using System.IO;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.GauntletUI;
using TaleWorlds.ScreenSystem;
using HannibalAI.UI;
using HannibalAI.Services;
using HannibalAI.Config;
using Newtonsoft.Json;
using HarmonyLib;

namespace HannibalAI
{
    public class SubModule : MBSubModuleBase
    {
        private static ModConfig _config;
        private static AIService _aiService;
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            "Mount and Blade II Bannerlord",
            "Configs",
            "HannibalAI"
        );

        protected override void OnSubModuleLoad()
        {
            try
            {
                _config = ModConfig.Load();
                _aiService = new AIService(_config.AIEndpoint, _config.APIKey);
                
                // Initialize Harmony patches
                var harmony = new Harmony("com.hannibalai.patches");
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                Debug.Print($"Error loading HannibalAI: {ex.Message}");
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            try
            {
                if (gameStarterObject is CampaignGameStarter campaignStarter)
                {
                    InitializeGameMenus(campaignStarter);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Error initializing HannibalAI: {ex.Message}");
            }
        }

        private void InitializeGameMenus(CampaignGameStarter gameStarter)
        {
            try
            {
                // Add mod settings to campaign menu
                gameStarter.AddGameMenu(
                    "hannibal_ai_settings",
                    "Hannibal AI Settings",
                    (args) => { },
                    GameOverlays.MenuOverlayType.None
                );

                gameStarter.AddGameMenuOption(
                    "campaign",
                    "hannibal_ai_settings",
                    "Hannibal AI Settings",
                    (args) => true,
                    (args) => OnSettingsSelected(args),
                    false,
                    0,
                    false
                );
            }
            catch (Exception ex)
            {
                Debug.Print($"Error adding menu items: {ex.Message}");
            }
        }

        private void OnSettingsSelected(MenuCallbackArgs args)
        {
            try
            {
                var mission = Mission.Current;
                if (mission != null)
                {
                    var settingsView = new ModSettingsView(mission, _aiService);
                    ScreenManager.PushScreen(settingsView);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Error showing settings: {ex.Message}");
            }
        }

        public static ModConfig GetConfig()
        {
            return _config ?? ModConfig.Load();
        }

        public static AIService GetAIService()
        {
            return _aiService;
        }

        public static string GetConfigPath()
        {
            return ConfigPath;
        }
    }
} 