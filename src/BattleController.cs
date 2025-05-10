using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Engine;
// Use TaleWorlds.Core.MissionMode instead of TaleWorlds.MountAndBlade.MissionMode
using MissionMode = TaleWorlds.Core.MissionMode;
using HannibalAI.Tactics;
using HannibalAI.Terrain;
using HannibalAI.Memory;
// Explicitly specify the TerrainType from our namespace to avoid ambiguity
using OurTerrainType = HannibalAI.Terrain.TerrainType;

namespace HannibalAI
{
    /// <summary>
    /// Main battle controller that hooks into Bannerlord's mission system
    /// and delegates AI control to the AICommander
    /// </summary>
    public class BattleController : MissionBehavior
    {
        private readonly AIService _aiService;
        private AICommander _aiCommander;
        private bool _isInitialized;
        private bool _isPlayerInPlayerTeam;
        private bool _isBattleOver;
        private Team _playerTeam;
        private Team _enemyTeam;

        private float _updateTimer;
        private const float UPDATE_INTERVAL = 3.0f; // seconds between AI updates

        public BattleController(AIService aiService)
        {
            _aiService = aiService;
            _isInitialized = false;
            _isBattleOver = false;
            _updateTimer = 0f;
        }

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        /// <summary>
        /// Called when the mission is loaded
        /// </summary>
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            
            // Tick counter to limit debug output
            _updateTimer += dt;
            
            // Use the mission time to determine when to log
            float currentTime = Mission.Current?.CurrentTime ?? 0f;
            
            // Only output basic debug info every 3 seconds to reduce log spam
            // or during the first 5 seconds for initialization diagnostics
            bool shouldLogDetails = currentTime < 5.0f || _updateTimer >= 3.0f;
            
            // Reset timer if it's time to log details
            if (shouldLogDetails && _updateTimer >= 3.0f)
            {
                _updateTimer = 0f;
            }
            
            // Enhanced debug output to confirm OnMissionTick is firing only in verbose mode or on occasional ticks
            if (shouldLogDetails && ModConfig.Instance != null && ModConfig.Instance.VerboseLogging)
            {
                System.Diagnostics.Debug.Print("[HannibalAI] OnMissionTick fired.");
                Logger.Instance.Info("[HannibalAI] OnMissionTick firing regularly.");
            }
            
            // Display to player every 5 seconds if in debug mode
            if (ModConfig.Instance != null && ModConfig.Instance.Debug && Mission.Current.CurrentTime % 5 < 0.1f)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"HannibalAI: Controller active [{Mission.Current.CurrentTime:F0}s]", 
                    Color.FromUint(0x88FF88U)));
            }
            
            // Only log diagnostic information occasionally
            if (shouldLogDetails)
            {
                // Debug print to check registered mission behaviors
                System.Diagnostics.Debug.Print($"[HannibalAI] Registered in MissionBehaviors: {Mission.Current.MissionBehaviors.Count}");
                
                if (ModConfig.Instance != null && ModConfig.Instance.VerboseLogging)
                {
                    // Log basic mission state
                    Logger.Instance.Info($"[HannibalAI] Mission.Current time: {Mission.Current.CurrentTime:F2}, Mission behaviors: {Mission.Current.MissionBehaviors.Count}");
                    
                    // Debug print to check if AI is configured to control enemy formations
                    Logger.Instance.Info($"[HannibalAI] AIControlsEnemies setting: {ModConfig.Instance.AIControlsEnemies}");
                    
                    // Add battle state information
                    if (_playerTeam != null && _enemyTeam != null)
                    {
                        int playerFormations = _playerTeam.FormationsIncludingEmpty?.Count ?? 0;
                        int enemyFormations = _enemyTeam.FormationsIncludingEmpty?.Count ?? 0;
                        Logger.Instance.Info($"[HannibalAI] Battle state: Player team: {playerFormations} formations, Enemy team: {enemyFormations} formations");
                        
                        // Count active formations with units
                        int activePlayerFormations = 0;
                        int totalPlayerUnits = 0;
                        foreach (var formation in _playerTeam.FormationsIncludingEmpty)
                        {
                            if (formation != null && formation.CountOfUnits > 0)
                            {
                                activePlayerFormations++;
                                totalPlayerUnits += formation.CountOfUnits;
                            }
                        }
                        
                        // Include AI Commander status
                        Logger.Instance.Info($"[HannibalAI] Player active formations: {activePlayerFormations} with {totalPlayerUnits} total units. AI Commander initialized: {(_aiCommander != null)}");
                    }
                }
            }
            
            // Log mission type for debugging
            if (!_isInitialized)
            {
                Logger.Instance.Info($"[HannibalAI] Mission mode: {Mission.Current.Mode}");
                Logger.Instance.Info($"[HannibalAI] Combat type: {Mission.Current.CombatType}");
                
                // Log the mission behaviors to help diagnose integration issues
                Logger.Instance.Info("[HannibalAI] Mission behaviors:");
                int index = 0;
                foreach (var behavior in Mission.Current.MissionBehaviors)
                {
                    Logger.Instance.Info($"  {index++}: {behavior.GetType().Name}");
                }
            }

            if (!_isInitialized)
            {
                InitializeAI();
                return;
            }

            if (_isBattleOver || _aiCommander == null)
            {
                return;
            }

            // Update AI at regular intervals
            _updateTimer += dt;
            if (_updateTimer >= UPDATE_INTERVAL)
            {
                _updateTimer = 0f;
                
                // Add debug log to verify AI is running
                if (_aiService.Config.Debug)
                {
                    Logger.Instance.Info("[HannibalAI] BattleController.OnMissionTick - Updating AI");
                    InformationManager.DisplayMessage(new InformationMessage(
                        "HannibalAI: Updating battle tactics", 
                        Color.FromUint(0x00CCFF)));
                }
                
                UpdateAI(dt);
            }

            // Check if battle is over
            if (IsBattleOver())
            {
                _isBattleOver = true;
            }
        }

        /// <summary>
        /// Initialize the AI when the battle begins
        /// </summary>
        private void InitializeAI()
        {
            try
            {
                System.Diagnostics.Debug.Print("[HannibalAI] Initializing AI...");
                
                // Find player and enemy teams
                Mission mission = Mission.Current;
                if (mission == null || mission.Teams == null || mission.Teams.Count < 2)
                {
                    System.Diagnostics.Debug.Print("[HannibalAI] Mission or teams not ready yet.");
                    return;
                }

                _playerTeam = null;
                _enemyTeam = null;

                foreach (Team team in mission.Teams)
                {
                    if (team.Side == BattleSideEnum.Defender)
                    {
                        _playerTeam = team;
                    }
                    else if (team.Side == BattleSideEnum.Attacker)
                    {
                        _enemyTeam = team;
                    }
                }

                // Ensure we found both teams
                if (_playerTeam == null || _enemyTeam == null)
                {
                    return;
                }

                // Check if player is in player team
                _isPlayerInPlayerTeam = IsPlayerInTeam(_playerTeam);

                // Analyze terrain features
                Terrain.TerrainAnalyzer.Instance.AnalyzeCurrentTerrain();
                
                // Get terrain information for tactical assessment
                HannibalAI.Terrain.TerrainType battlefieldType = Terrain.TerrainAnalyzer.Instance.GetTerrainType();
                bool hasTerrainAdvantage = Terrain.TerrainAnalyzer.Instance.HasTerrainAdvantage();
                
                // Log terrain analysis results
                Logger.Instance.Info($"Battle terrain type: {battlefieldType}");
                Logger.Instance.Info($"Terrain advantage: {(hasTerrainAdvantage ? "Yes" : "No")}");
                
                // Get tactical positions
                Vec3 bestHighGround = Terrain.TerrainAnalyzer.Instance.GetBestHighGroundPosition();
                Vec3 bestDefensivePosition = Terrain.TerrainAnalyzer.Instance.GetBestDefensivePosition();
                Vec3 rightFlankPosition = Terrain.TerrainAnalyzer.Instance.GetBestFlankingPosition(true);
                Vec3 leftFlankPosition = Terrain.TerrainAnalyzer.Instance.GetBestFlankingPosition(false);
                
                // Track initialization start time for profiling
                float initStartTime = Mission.Current?.CurrentTime ?? 0f;
                Logger.Instance.Info("Beginning AI commander initialization");
                System.Diagnostics.Debug.Print("[HannibalAI] Beginning AI commander initialization");
                
                // Initialize AI commander with terrain information
                _aiCommander = _aiService.CreateCommander();
                Logger.Instance.Info($"AI commander instance created: {(_aiCommander != null ? "Success" : "Failed")}");
                
                // Initialize the commander with teams
                _aiCommander.Initialize(_playerTeam, _enemyTeam);
                Logger.Instance.Info($"AI commander initialized with player team (size: {_playerTeam?.FormationsIncludingEmpty?.Count ?? 0}) and enemy team (size: {_enemyTeam?.FormationsIncludingEmpty?.Count ?? 0})");
                
                // Build tactical positions dictionary
                Dictionary<string, Vec3> tacticalPositions = new Dictionary<string, Vec3>
                {
                    { "HighGround", bestHighGround },
                    { "DefensivePosition", bestDefensivePosition },
                    { "RightFlank", rightFlankPosition },
                    { "LeftFlank", leftFlankPosition }
                };
                
                // Pass tactical positions to AI commander
                _aiCommander.SetTacticalPositions(tacticalPositions);
                Logger.Instance.Info($"Tactical positions set: {tacticalPositions.Count} positions");
                
                // Set battlefield type for AI decision making
                _aiCommander.SetBattlefieldType(battlefieldType);
                Logger.Instance.Info($"Battlefield type set: {battlefieldType}");
                
                // Inform the AI commander if we have terrain advantage
                _aiCommander.SetTerrainAdvantage(hasTerrainAdvantage);
                Logger.Instance.Info($"Terrain advantage set: {hasTerrainAdvantage}");
                
                // Track initialization end time for profiling
                float initEndTime = Mission.Current?.CurrentTime ?? 0f;
                float initDuration = initEndTime - initStartTime;
                Logger.Instance.Info($"AI commander initialization completed in {initDuration:F3} seconds");
                System.Diagnostics.Debug.Print($"[HannibalAI] AI commander initialization completed in {initDuration:F3} seconds");

                _isInitialized = true;

                // Log initialization and debug info
                Logger.Instance.Info("HannibalAI Controller initialized for battle");
                Logger.Instance.Info($"[HannibalAI] AIControlsEnemies (at BattleController init): {ModConfig.Instance.AIControlsEnemies}");
                Logger.Instance.Info($"[HannibalAI] Registered in MissionBehaviors: {Mission.Current.MissionBehaviors.Count}");

                // Display enemy AI control status
                string aiControlStatus = ModConfig.Instance.AIControlsEnemies ? 
                    "HannibalAI is controlling both friendly and enemy formations" : 
                    "HannibalAI is controlling friendly formations only";
                Logger.Instance.Info(aiControlStatus);

                InformationManager.DisplayMessage(new InformationMessage(
                    "HannibalAI activated!", Color.FromUint(0x00FF00)));

                // Show detailed status messages
                InformationManager.DisplayMessage(new InformationMessage(
                    $"HannibalAI Active: {(ModConfig.Instance.AIControlsEnemies ? "Controlling All Forces" : "Friendly Forces Only")}", Color.FromUint(0x00FF00)));
                
                if (ModConfig.Instance.Debug)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Debug Mode: ON - Press INSERT for settings", Color.FromUint(0xFFFF00)));
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Formations controlled: {_playerTeam.FormationsIncludingEmpty.Count}", Color.FromUint(0xFFFF00)));
                }

                // Log terrain features if in debug mode
                if (ModConfig.Instance.Debug)
                {
                    // Log tactical positions
                    Logger.Instance.Info($"Terrain Analysis: Key Positions Found");
                    Logger.Instance.Info($"  - High Ground: ({bestHighGround.x:F1}, {bestHighGround.y:F1}, {bestHighGround.z:F1})");
                    Logger.Instance.Info($"  - Defensive Position: ({bestDefensivePosition.x:F1}, {bestDefensivePosition.y:F1}, {bestDefensivePosition.z:F1})");
                    Logger.Instance.Info($"  - Right Flank: ({rightFlankPosition.x:F1}, {rightFlankPosition.y:F1}, {rightFlankPosition.z:F1})");
                    Logger.Instance.Info($"  - Left Flank: ({leftFlankPosition.x:F1}, {leftFlankPosition.y:F1}, {leftFlankPosition.z:F1})");
                    
                    // Display terrain advantage in game
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Terrain assessment: {(hasTerrainAdvantage ? "Advantage" : "Standard")} - {battlefieldType} terrain", 
                        Color.FromUint(hasTerrainAdvantage ? 0x88FF88U : 0xFFFFAAU)));
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error initializing HannibalAI Controller: {ex.Message}");
            }
        }

        /// <summary>
        /// Update the AI state and process commands
        /// </summary>
        private void UpdateAI(float dt)
        {
            try
            {
                // Track performance of AI update
                float startTime = Mission.Current.CurrentTime;
                
                // Debug performance tracking
                if (ModConfig.Instance.Debug)
                {
                    Logger.Instance.Info($"[HannibalAI] Starting AI update at {startTime:F2}s");
                    System.Diagnostics.Debug.Print($"[HannibalAI] Starting AI update at {startTime:F2}s");
                }
                
                // Display active AI status at battle start for player awareness
                if (Mission.Current.CurrentTime < 1.0f)
                {
                    // First notification - large and noticeable
                    InformationManager.DisplayMessage(new InformationMessage(
                        "=== HANNIBAL AI ACTIVE ===", Color.FromUint(0xFFCC00U)));
                    
                    Logger.Instance.Info("HannibalAI is running in this battle");
                    
                    // Log AI settings and configuration for debugging
                    Logger.Instance.Info($"[HannibalAI] Debug mode: {ModConfig.Instance.Debug}, Verbose logging: {ModConfig.Instance.VerboseLogging}");
                    Logger.Instance.Info($"[HannibalAI] AI Controls player formations: true, AI Controls enemy formations: {ModConfig.Instance.AIControlsEnemies}");
                    Logger.Instance.Info($"[HannibalAI] UseTerrainAnalysis: {ModConfig.Instance.UseTerrainAnalysis}, UseCommanderMemory: {ModConfig.Instance.UseCommanderMemory}");
                }
                
                // Second notification with more details
                if (Mission.Current.CurrentTime > 2.0f && Mission.Current.CurrentTime < 2.5f)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "HannibalAI active - Press INSERT for settings", Color.FromUint(0x33FF33U)));
                }
                
                // Update Tactical Planner with both original AICommander and enhanced Tactical system
                
                // 1. Always run original AICommander for player's team
                if (_isPlayerInPlayerTeam)
                {
                    // Call the original AI's decision-making process
                    _aiCommander.MakeDecision();
                    
                    // Then update ongoing AI activities
                    _aiCommander.Update(dt);
                    
                    // Add verbose logging to help troubleshoot AI command execution
                    System.Diagnostics.Debug.Print("[HannibalAI] AI commands executed for player team");
                    
                    if (ModConfig.Instance.Debug)
                    {
                        Logger.Instance.Info($"[HannibalAI] Player team has {_playerTeam.FormationsIncludingEmpty.Count} formations");
                    }
                    if (ModConfig.Instance.Debug && Mission.Current.CurrentTime % 15 < 0.1f)
                    {
                        Logger.Instance.Info("HannibalAI friendly commander update executed");
                    }
                }
                
                // 2. Control enemy team if enabled in settings (using enhanced tactical system)
                if (ModConfig.Instance.AIControlsEnemies && _enemyTeam != null)
                {
                    // Debug logging for enemy AI control
                    System.Diagnostics.Debug.Print("[HannibalAI] Processing enemy AI. AIControlsEnemies=true");
                    
                    // Get tactical plan from tactical planner
                    TacticalPlan enemyPlan = TacticalPlanner.Instance.DevelopTacticalPlan(_enemyTeam, _playerTeam);
                    
                    // Execute the tactical plan
                    ExecuteTacticalPlan(enemyPlan);
                    
                    // Debug information about enemy formations
                    System.Diagnostics.Debug.Print($"[HannibalAI] Enemy team has {_enemyTeam.FormationsIncludingEmpty.Count} formations");
                    
                    if (ModConfig.Instance.Debug)
                    {
                        Logger.Instance.Info($"[HannibalAI] Executed tactical plan for enemy team with {_enemyTeam.FormationsIncludingEmpty.Count} formations");
                    }
                    
                    // Record commander memory data for adaptive learning
                    if (ModConfig.Instance.UseCommanderMemory)
                    {
                        // Record tactical data for enemy team
                        Dictionary<FormationClass, int> enemyUnitCounts = GetUnitCounts(_enemyTeam);
                        // Would record player unit composition for AI to learn
                        // CommanderMemoryService.Instance.RecordPlayerUnitComposition(enemyUnitCounts);
                        Logger.Instance.Info("Would record player unit composition for AI learning");
                    }
                    
                    // Display enemy AI status
                    if (Mission.Current.CurrentTime < 1.0f)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            "HannibalAI is controlling enemy formations", Color.FromUint(0xFF3333U)));
                    }
                    
                    // Repeat notification after 3 seconds for visibility
                    if (Mission.Current.CurrentTime > 3.0f && Mission.Current.CurrentTime < 3.5f)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"HannibalAI active - Enemy AI using {enemyPlan.Strategy} tactics", 
                            Color.FromUint(0xFF3333U)));
                    }

                    // Show enemy AI control status periodically (every 30 seconds)
                    if (ModConfig.Instance.Debug && Mission.Current.CurrentTime % 30 < 1.0f)
                    {
                        // Get information about the commander
                        string enemyCommanderInfo = "";
                        if (ModConfig.Instance.UseCommanderMemory)
                        {
                            // Get commander type from memory service
                            string commanderType = "Standard"; // Simplified for compatibility
                            float aggression = 0.5f; // Default value for compatibility
                            string style = aggression > 0.7f ? "Aggressive" : (aggression < 0.3f ? "Cautious" : "Balanced");
                            enemyCommanderInfo = $" (Commander: {commanderType} - {style})";
                        }

                        // Display as game message so player can see it
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"Enemy Tactic: {enemyPlan.Strategy}{enemyCommanderInfo}", Color.FromUint(0xFF6600U)));
                        
                        // Log additional details when in debug mode
                        if (ModConfig.Instance.Debug)
                        {
                            string formationDetails = "";
                            foreach (var action in enemyPlan.FormationActions)
                            {
                                formationDetails += $"\n  {action.Formation.FormationIndex}: {action.ActionType} ({action.FormationType})";
                            }
                            
                            Logger.Instance.Info($"HannibalAI enemy plan: {enemyPlan.Strategy}{enemyCommanderInfo}{formationDetails}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error in HannibalAI update: {ex.Message}\n{ex.StackTrace}");
                
                // Display error to player so they're aware something went wrong
                InformationManager.DisplayMessage(new InformationMessage(
                    $"HannibalAI Error: {ex.Message}", Color.FromUint(0xFF0000U)));
            }
        }
        
        /// <summary>
        /// Execute a tactical plan by issuing formation orders
        /// </summary>
        private void ExecuteTacticalPlan(TacticalPlan plan)
        {
            try
            {
                if (plan == null || plan.FormationActions.Count == 0)
                {
                    return;
                }
                
                // Sort actions by priority (lowest number = highest priority)
                plan.FormationActions.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                
                // Execute each action
                foreach (var action in plan.FormationActions)
                {
                    if (action.Formation == null || action.Formation.CountOfUnits <= 0)
                    {
                        continue;
                    }
                    
                    // Apply formation type if specified
                    if (!string.IsNullOrEmpty(action.FormationType))
                    {
                        ApplyFormationType(action.Formation, action.FormationType);
                    }
                    
                    // Execute the action
                    switch (action.ActionType)
                    {
                        case FormationActionType.Hold:
                            // Order the formation to hold position
                            Logger.Instance.Info($"Ordering formation {action.Formation.FormationIndex} to hold position");
                            // Use direct formation methods instead of OrderController
                            action.Formation.SetMovementOrder(MovementOrder.MovementOrderStop);
                            break;
                            
                        case FormationActionType.Advance:
                            WorldPosition targetPos = new WorldPosition(Mission.Current.Scene, action.TargetPosition);
                            Logger.Instance.Info($"Ordering formation {action.Formation.FormationIndex} to advance to position {action.TargetPosition}");
                            // Use direct formation method with WorldPosition
                            if (targetPos.GetNavMesh() != null)
                            {
                                action.Formation.SetMovementOrder(MovementOrder.MovementOrderMove(targetPos));
                            }
                            break;
                            
                        case FormationActionType.Charge:
                            Logger.Instance.Info($"Ordering formation {action.Formation.FormationIndex} to charge");
                            // Use direct formation method for charging
                            action.Formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                            break;
                            
                        case FormationActionType.Retreat:
                            WorldPosition retreatPos = new WorldPosition(Mission.Current.Scene, action.TargetPosition);
                            Logger.Instance.Info($"Ordering formation {action.Formation.FormationIndex} to retreat to position {action.TargetPosition}");
                            // If no retreat position is specified, use general retreat
                            if (retreatPos.GetNavMesh() != null)
                            {
                                // Use a fallback with regular movement to a retreat position
                                action.Formation.SetMovementOrder(MovementOrder.MovementOrderMove(retreatPos));
                            }
                            else
                            {
                                // Just regular retreat
                                action.Formation.SetMovementOrder(MovementOrder.MovementOrderRetreat);
                            }
                            break;
                            
                        case FormationActionType.FireAt:
                            WorldPosition firePos = new WorldPosition(Mission.Current.Scene, action.TargetPosition);
                            Logger.Instance.Info($"Ordering formation {action.Formation.FormationIndex} to fire at position {action.TargetPosition}");
                            // First ensure units are holding position
                            action.Formation.SetMovementOrder(MovementOrder.MovementOrderStop);
                            
                            // Get direction vector for formation to face
                            // Create a world position for the target and get ground positions
                            WorldPosition targetWorldPos = new WorldPosition(Mission.Current.Scene, action.TargetPosition);
                            Vec3 targetGroundPos = targetWorldPos.GetGroundVec3();
                            
                            // Create a proper world position from formation position (which is Vec2)
                            Vec3 formationPos3D = new Vec3(action.Formation.CurrentPosition.x, action.Formation.CurrentPosition.y, 0f);
                            WorldPosition formationWorldPos = new WorldPosition(Mission.Current.Scene, formationPos3D);
                            Vec3 formationGroundPos = formationWorldPos.GetGroundVec3();
                            
                            // Create direction vector as Vec2 from the Vec3 positions (ignore height)
                            Vec2 direction = new Vec2(
                                targetGroundPos.x - formationGroundPos.x, 
                                targetGroundPos.y - formationGroundPos.y);
                            direction.Normalize();
                            
                            // Set facing order directly
                            action.Formation.FacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
                            break;
                            
                        case FormationActionType.Flank:
                            WorldPosition flankPos = new WorldPosition(Mission.Current.Scene, action.TargetPosition);
                            Logger.Instance.Info($"Ordering formation {action.Formation.FormationIndex} to flank to position {action.TargetPosition}");
                            // Use advance order to flank position (simplified implementation)
                            if (flankPos.GetNavMesh() != null)
                            {
                                action.Formation.SetMovementOrder(MovementOrder.MovementOrderMove(flankPos));
                            }
                            break;
                            
                        case FormationActionType.Harass:
                            WorldPosition harassPos = new WorldPosition(Mission.Current.Scene, action.TargetPosition);
                            Logger.Instance.Info($"Ordering formation {action.Formation.FormationIndex} to harass at position {action.TargetPosition}");
                            // First move to position
                            if (harassPos.GetNavMesh() != null)
                            {
                                action.Formation.SetMovementOrder(MovementOrder.MovementOrderMove(harassPos));
                            }
                            break;
                            
                        case FormationActionType.Guard:
                            WorldPosition guardPos = new WorldPosition(Mission.Current.Scene, action.TargetPosition);
                            Logger.Instance.Info($"Ordering formation {action.Formation.FormationIndex} to guard position {action.TargetPosition}");
                            // Move to guard position and hold
                            if (guardPos.GetNavMesh() != null)
                            {
                                action.Formation.SetMovementOrder(MovementOrder.MovementOrderMove(guardPos));
                            }
                            break;
                    }
                    
                    // Log action execution in debug mode
                    if (ModConfig.Instance.Debug)
                    {
                        Logger.Instance.Info($"Formation {action.Formation.FormationIndex} executing {action.ActionType} with formation type {action.FormationType}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing tactical plan: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Apply a formation arrangement type to a formation
        /// </summary>
        private void ApplyFormationType(Formation formation, string formationType)
        {
            try
            {
                if (formation == null || string.IsNullOrEmpty(formationType))
                {
                    return;
                }
                
                // Get the appropriate ArrangementOrder based on formation type
                // Default to line formation
                var arrangementOrder = ArrangementOrder.ArrangementOrderLine;
                
                switch (formationType.ToLower())
                {
                    case "line":
                        arrangementOrder = ArrangementOrder.ArrangementOrderLine;
                        break;
                    case "close":
                    case "tightformation":
                        // Use line formation but with closer spacing
                        arrangementOrder = ArrangementOrder.ArrangementOrderLine;
                        // Would set spacing tighter if API supported it
                        break;
                    case "loose":
                        arrangementOrder = ArrangementOrder.ArrangementOrderLoose;
                        break;
                    case "circle":
                        arrangementOrder = ArrangementOrder.ArrangementOrderCircle;
                        break;
                    case "square":
                    case "schiltron":
                        // Use defensive circle as fallback
                        arrangementOrder = ArrangementOrder.ArrangementOrderCircle;
                        break;
                    case "column":
                        // Use column formation
                        arrangementOrder = ArrangementOrder.ArrangementOrderColumn;
                        break;
                    case "shieldwall":
                        // Use shieldwall if available
                        arrangementOrder = ArrangementOrder.ArrangementOrderShieldWall;
                        break;
                    case "wedge":
                        // Use skein as wedge
                        arrangementOrder = ArrangementOrder.ArrangementOrderSkein;
                        break;
                    case "skein":
                        // Use skein formation
                        arrangementOrder = ArrangementOrder.ArrangementOrderSkein;
                        break;
                    case "scatter":
                        arrangementOrder = ArrangementOrder.ArrangementOrderScatter;
                        break;
                    default:
                        // Default to line if unknown
                        arrangementOrder = ArrangementOrder.ArrangementOrderLine;
                        break;
                }
                
                // Apply the formation arrangement directly
                Logger.Instance.Info($"Setting formation type to {formationType}");
                formation.ArrangementOrder = arrangementOrder;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error applying formation type: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get unit counts for each formation class in a team
        /// </summary>
        private Dictionary<FormationClass, int> GetUnitCounts(Team team)
        {
            var counts = new Dictionary<FormationClass, int>();
            
            try
            {
                foreach (FormationClass formationClass in Enum.GetValues(typeof(FormationClass)))
                {
                    if (formationClass != FormationClass.NumberOfAllFormations)
                    {
                        counts[formationClass] = 0;
                    }
                }
                
                foreach (Formation formation in team.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits > 0 && formation.FormationIndex != FormationClass.NumberOfAllFormations)
                    {
                        counts[formation.FormationIndex] += formation.CountOfUnits;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error getting unit counts: {ex.Message}");
            }
            
            return counts;
        }

        /// <summary>
        /// Check if player is in the given team
        /// </summary>
        private bool IsPlayerInTeam(Team team)
        {
            if (team == null || Agent.Main == null)
            {
                return false;
            }

            return Agent.Main.Team == team;
        }

        /// <summary>
        /// Check if the battle is effectively over
        /// </summary>
        private bool IsBattleOver()
        {
            if (_playerTeam == null || _enemyTeam == null)
            {
                return true;
            }

            // Check if any team has no active agents
            return _playerTeam.ActiveAgents.Count == 0 || _enemyTeam.ActiveAgents.Count == 0;
        }

        /// <summary>
        /// Execute AI decisions (formation orders)
        /// </summary>
        private void ExecuteAIDecisions(List<FormationOrder> orders, float aggressionFactor)
        {
            if (orders == null || orders.Count == 0)
            {
                return;
            }

            try
            {
                // Execute each order
                foreach (var order in orders)
                {
                    // Skip if formation is invalid
                    if (order.TargetFormation == null || order.TargetFormation.CountOfUnits <= 0)
                    {
                        continue;
                    }

                    // Skip enemy formations if AI control is disabled
                    if (order.TargetFormation.Team.IsEnemyOf(Mission.Current.MainAgent.Team) && 
                        !ModConfig.Instance.AIControlsEnemies)
                    {
                        continue;
                    }

                    // Execute the order, potentially modifying it based on aggressionFactor
                    CommandExecutor.Instance.ExecuteOrder(order, aggressionFactor); //Added aggressionFactor


                    // Log if debug is enabled
                    if (ModConfig.Instance.Debug)
                    {
                        string message = $"Order: {order.OrderType} to formation {order.TargetFormation.Index}";
                        Logger.Instance.Debug(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing AI decisions: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the mission ends
        /// </summary>
        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            base.OnMissionModeChange(oldMissionMode, atStart);

            // Check if mission is ending (using enum value instead of End property)
            if (Mission.Current.Mode == MissionMode.Battle)
            {
                // Mission is in battle mode, continue
            }
            else if (Mission.Current.Mode == MissionMode.Deployment)
            {
                // Battle is starting, initialize memory
                Logger.Instance.Info("Battle is starting, initializing commander memory");
            }
            else
            {
                _isBattleOver = true;
                
                // Record battle outcome when the mission ends
                if (ModConfig.Instance.UseCommanderMemory && _playerTeam != null && _enemyTeam != null)
                {
                    bool playerVictory = false;
                    int playerCasualties = 0;
                    int enemyCasualties = 0;
                    
                    // Determine victory based on remaining troops
                    if (_playerTeam.ActiveAgents.Count > 0 && _enemyTeam.ActiveAgents.Count == 0)
                    {
                        playerVictory = true;
                    }
                    
                    // Count casualties (simplified for compatibility)
                    // Use InitialAgentCount or estimate based on battle situation
                    int playerInitialCount = Math.Max(100, _playerTeam.ActiveAgents.Count + playerCasualties);
                    int enemyInitialCount = Math.Max(100, _enemyTeam.ActiveAgents.Count + enemyCasualties);
                    
                    // Calculate casualties based on initial estimates and remaining troops
                    playerCasualties = playerInitialCount - _playerTeam.ActiveAgents.Count;
                    enemyCasualties = enemyInitialCount - _enemyTeam.ActiveAgents.Count;
                    
                    // Record the outcome
                    CommanderMemoryService.Instance.RecordBattleOutcome(!playerVictory, enemyCasualties, playerCasualties);
                    
                    Logger.Instance.Info($"Battle outcome recorded: Player victory: {playerVictory}, " +
                        $"Player casualties: {playerCasualties}, Enemy casualties: {enemyCasualties}");
                }
            }
        }
    }
}