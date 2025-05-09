using System;
using System.IO;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using HannibalAI.UI;

namespace HannibalAI
{
    public class SubModule : MBSubModuleBase
    {
        private AIService _aiService;
        private ModConfig _config;
        private string _logPath;
        private static bool _hasLoggedGreeting = false;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                // Initialize logging
                _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), 
                    "Mount and Blade II Bannerlord", "Logs", "HannibalAI.log");
                
                // Ensure log directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(_logPath));
                
                // Initialize basic logger
                Logger.Initialize(_logPath);
                Logger.Instance.Info("HannibalAI mod starting up...");
                
                // Initialize mod configuration
                _config = new ModConfig();
                _config.LoadSettings();
                
                // Initialize UI system
                Logger.Instance.Info($"Mod config loaded - AI Controls Enemies: {_config.AIControlsEnemies}, " +
                    $"Use Commander Memory: {_config.UseCommanderMemory}, Debug: {_config.Debug}");
                
                // Register with screen manager if possible
                if (ScreenManagerIntegration.RegisterModSettingsScreen())
                {
                    Logger.Instance.Info("Successfully registered with the game's screen manager");
                }
                else
                {
                    Logger.Instance.Warning("Screen manager registration failed, will use in-mission UI only");
                }

                // Log mod initialization
                InformationManager.DisplayMessage(new InformationMessage("HannibalAI is loading..."));
            }
            catch (Exception ex)
            {
                // Make sure we log errors even during initialization
                if (Logger.Instance != null)
                {
                    Logger.Instance.Error($"Error in OnSubModuleLoad: {ex.Message}\n{ex.StackTrace}");
                }
                else
                {
                    // Fallback if logger isn't available yet
                    File.AppendAllText(_logPath, $"[ERROR] {DateTime.Now}: Error in OnSubModuleLoad: {ex.Message}\n{ex.StackTrace}\n");
                }
                
                InformationManager.DisplayMessage(new InformationMessage($"Error initializing HannibalAI: {ex.Message}", Colors.Red));
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            try
            {
                // Initialize the AI service
                _aiService = new AIService(_config);
                Logger.Instance.Info("AI Service initialized successfully");

                // Initialize memory service (it auto-initializes via Instance property)
                Logger.Instance.Info("Commander Memory Service initialized");

                // Register key handlers for settings menu
                RegisterInputKeys();

                // Print success message
                InformationManager.DisplayMessage(new InformationMessage("HannibalAI has been initialized successfully!", Colors.Green));
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error in OnBeforeInitialModuleScreenSetAsRoot: {ex.Message}\n{ex.StackTrace}");
                InformationManager.DisplayMessage(new InformationMessage($"Error initializing HannibalAI AI service: {ex.Message}", Colors.Red));
            }
        }
        
        // Register keyboard shortcuts
        private void RegisterInputKeys()
        {
            try
            {
                // We don't need to manually register keys as SettingsBehavior handles this in mission
                Logger.Instance.Info("Key bindings will be managed by SettingsBehavior");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error registering input keys: {ex.Message}");
            }
        }
        
        // Register mission behaviors for battles
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            
            try
            {
                // Add settings behavior to every mission for key handling
                mission.AddMissionBehavior(new SettingsBehavior());
                
                // Only add our battle controller to battles, not other mission types
                if (mission.CombatType == Mission.MissionCombatType.Combat)
                {
                    // Log mission init
                    Logger.Instance.Info($"Initializing HannibalAI for battle mission");
                    
                    // Add our battle controller as a mission behavior
                    mission.AddMissionBehavior(new BattleController(_aiService));
                    
                    // Log successful registration
                    Logger.Instance.Info("BattleController registered successfully");
                    
                    // Show a greeting message only once per session
                    if (!_hasLoggedGreeting)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            "HannibalAI: Tactical AI module is active. Press INSERT to access settings.", 
                            Colors.Green));
                        _hasLoggedGreeting = true;
                    }
                    
                    if (_config.VerboseLogging)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("HannibalAI is active in this battle"));
                    }
                    
                    // Log available commands
                    if (_config.Debug)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Press INSERT key to open HannibalAI settings"));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error adding battle controller: {ex.Message}\n{ex.StackTrace}");
                InformationManager.DisplayMessage(new InformationMessage(
                    $"HannibalAI Error: {ex.Message}", Colors.Red));
            }
        }
    }
}
