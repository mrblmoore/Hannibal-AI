using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;
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
                
                // Initialize AI commander
                _aiCommander = _aiService.CreateCommander();
                _aiCommander.Initialize(_playerTeam, _enemyTeam);
                
                _isInitialized = true;
                
                // Log initialization
                InformationManager.DisplayMessage(new InformationMessage("HannibalAI Controller initialized for battle"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error initializing HannibalAI Controller: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Update the AI state and process commands
        /// </summary>
        private void UpdateAI(float dt)
        {
            try
            {
                // Only control AI if player is in the player team
                if (_isPlayerInPlayerTeam)
                {
                    // Run the AI update
                    _aiCommander.Update(dt);
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in HannibalAI update: {ex.Message}"));
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
