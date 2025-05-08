using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Engine;
// Use TaleWorlds.Core.MissionMode instead of TaleWorlds.MountAndBlade.MissionMode
using MissionMode = TaleWorlds.Core.MissionMode;

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
                // Find player and enemy teams
                Mission mission = Mission.Current;
                if (mission == null || mission.Teams == null || mission.Teams.Count < 2)
                {
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
                var terrainFeatures = TerrainAnalyzer.Instance.AnalyzeCurrentTerrain();

                // Initialize AI commander
                _aiCommander = _aiService.CreateCommander();
                _aiCommander.Initialize(_playerTeam, _enemyTeam);

                _isInitialized = true;

                // Log initialization
                Logger.Instance.Info("HannibalAI Controller initialized for battle");

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
                        $"Debug Mode: ON - Press F5 for settings", Color.FromUint(0xFFFF00)));
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Formations controlled: {_playerTeam.FormationsCount}", Color.FromUint(0xFFFF00)));
                }

                // Log terrain features if in debug mode
                if (ModConfig.Instance.Debug && terrainFeatures.Count > 0)
                {
                    Logger.Instance.Info($"Terrain Analysis: Found {terrainFeatures.Count} tactical features");
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
                // Get formations to control based on settings
                if (_isPlayerInPlayerTeam)
                {
                    // Run the AI update for friendly team
                    _aiCommander.Update(dt);

                    // Control enemy team if enabled in settings
                    if (ModConfig.Instance.AIControlsEnemies && _enemyTeam != null)
                    {
                        // Determine tactical approach for enemy forces using our enhanced system
                        TacticalApproach enemyApproach = _aiService.DetermineTacticalApproach(_enemyTeam, _playerTeam);

                        // Process AI decisions for enemy formations
                        var enemyOrders = _aiService.ProcessBattleSnapshot(_enemyTeam, _playerTeam);

                        // Apply terrain and tactical modifiers to the orders
                        _aiService.ApplyTerrainTactics(enemyOrders, enemyApproach);

                        // Execute the enhanced orders with aggression factor
                        float aggressionFactor = CommanderMemoryService.Instance.AggressivenessScore;
                        ExecuteAIDecisions(enemyOrders, aggressionFactor);

                        // Show enemy AI control status periodically (every 30 seconds)
                        if (ModConfig.Instance.Debug && Mission.Current.CurrentTime % 30 < 1.0f)
                        {
                            string enemyCommanderInfo = "";
                            if (ModConfig.Instance.UseCommanderMemory)
                            {
                                float aggression = CommanderMemoryService.Instance.AggressivenessScore;
                                string style = aggression > 0.7f ? "Aggressive" : (aggression < 0.3f ? "Cautious" : "Balanced");
                                enemyCommanderInfo = $" (Commander: {style})";
                            }

                            string tacticName = enemyApproach.RecommendedTactic.ToString();
                            string advantages = "";

                            if (enemyApproach.HasHighGround) advantages += "High Ground, ";
                            if (enemyApproach.HasCavalryAdvantage) advantages += "Cavalry, ";
                            if (enemyApproach.HasArcherAdvantage) advantages += "Archers, ";
                            if (enemyApproach.HasForestCover) advantages += "Forest Cover, ";

                            if (advantages.Length > 2)
                            {
                                advantages = "Advantages: " + advantages.Substring(0, advantages.Length - 2);
                            }

                            Logger.Instance.Info($"HannibalAI is controlling enemy - Tactic: {tacticName}{enemyCommanderInfo} | {advantages}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error in HannibalAI update: {ex.Message}");
            }
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
            else
            {
                _isBattleOver = true;
            }
        }
    }
}