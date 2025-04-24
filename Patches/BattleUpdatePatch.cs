using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using HannibalAI.Battle;
using HannibalAI.Services;
using HannibalAI.Command;
using System.IO;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace HannibalAI.Patches
{
    [HarmonyPatch(typeof(Mission), "OnTick")]
    public class BattleUpdatePatch
    {
        private static readonly AIService _aiService;
        private static float _lastUpdateTime = 0f;
        private static readonly float UPDATE_INTERVAL = 1.0f;
        private static AIDecision _lastDecision;
        private static bool _battleStarted = false;
        private static string _currentCommanderId;
        private static readonly string LogFile = "hannibal_ai_errors.log";

        static BattleUpdatePatch()
        {
            try
            {
                var config = SubModule.GetConfig();
                _aiService = new AIService(config.AIEndpoint, config.APIKey);
            }
            catch (Exception ex)
            {
                LogError($"Error initializing BattleUpdatePatch: {ex.Message}");
                throw;
            }
        }

        [HarmonyPostfix]
        public static async void Postfix(Mission __instance, float dt)
        {
            try
            {
                if (!__instance.IsFieldBattle || !__instance.IsActive)
                    return;

                // Track battle start
                if (!_battleStarted && IsBattleActive(__instance))
                {
                    _battleStarted = true;
                    _currentCommanderId = __instance.PlayerEnemyTeam?.Leader?.Id.ToString() ?? "unknown";
                    LogInfo($"Battle started with commander: {_currentCommanderId}");
                }

                // Update AI decision making
                _lastUpdateTime += dt;
                if (_lastUpdateTime >= UPDATE_INTERVAL)
                {
                    _lastUpdateTime = 0f;
                    await UpdateAI(__instance);
                }

                // Check for battle end
                if (_battleStarted && !IsBattleActive(__instance))
                {
                    HandleBattleEnd(__instance);
                    ResetBattleState();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in battle update: {ex.Message}");
            }
        }

        private static async Task UpdateAI(Mission mission)
        {
            try
            {
                var snapshot = BattleSnapshot.CreateFromMission(mission);
                if (snapshot == null) return;

                var decision = await _aiService.ProcessBattleSnapshot(snapshot);
                if (decision == null) return;

                _lastDecision = decision;

                foreach (var command in decision.Commands)
                {
                    try
                    {
                        // Extract formation type and other parameters
                        var parameters = new List<object>();
                        
                        // First parameter is always the formation type
                        if (command.Parameters?.Length > 0)
                        {
                            parameters.Add(command.Parameters[0]);
                            
                            // Add any additional parameters
                            for (int i = 1; i < command.Parameters.Length; i++)
                            {
                                parameters.Add(command.Parameters[i]);
                            }
                        }
                        else
                        {
                            // Default to infantry if no formation specified
                            parameters.Add("infantry");
                        }

                        CommandExecutor.ExecuteCommand(command.Value, mission, parameters.ToArray());
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error executing command {command.Value}: {ex.Message}");
                    }
                }

                if (SubModule.GetConfig().Debug)
                {
                    LogInfo($"Executed decision: {decision.Action} - {decision.Reasoning}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error in AI update: {ex.Message}");
            }
        }

        private static void HandleBattleEnd(Mission mission)
        {
            try
            {
                var snapshot = BattleSnapshot.CreateFromMission(mission);
                if (snapshot == null) return;

                bool victory = DetermineBattleOutcome(mission);
                _aiService.RecordBattleOutcome(_currentCommanderId, snapshot, _lastDecision, victory);

                LogInfo($"Battle ended. Commander {_currentCommanderId} {(victory ? "won" : "lost")}");
            }
            catch (Exception ex)
            {
                LogError($"Error handling battle end: {ex.Message}");
            }
        }

        private static bool IsBattleActive(Mission mission)
        {
            return mission.PlayerTeam != null && 
                   mission.PlayerEnemyTeam != null && 
                   mission.PlayerTeam.ActiveAgents.Any() && 
                   mission.PlayerEnemyTeam.ActiveAgents.Any();
        }

        private static bool DetermineBattleOutcome(Mission mission)
        {
            var playerTeam = mission.PlayerTeam;
            var enemyTeam = mission.PlayerEnemyTeam;

            if (playerTeam == null || enemyTeam == null)
                return false;

            // Victory if enemy team has no active agents or is retreating
            return !enemyTeam.ActiveAgents.Any() || 
                   enemyTeam.ActiveAgents.All(a => a.IsRunningAway);
        }

        private static void ResetBattleState()
        {
            _battleStarted = false;
            _lastDecision = null;
            _currentCommanderId = null;
            _lastUpdateTime = 0f;
        }

        private static void LogError(string message)
        {
            try
            {
                File.AppendAllText(LogFile, $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}\n");
                Debug.Print($"[HannibalAI] {message}");
            }
            catch { /* Ignore logging errors */ }
        }

        private static void LogInfo(string message)
        {
            try
            {
                if (SubModule.GetConfig().Debug)
                {
                    File.AppendAllText(LogFile, $"[INFO {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}\n");
                    Debug.Print($"[HannibalAI] {message}");
                }
            }
            catch { /* Ignore logging errors */ }
        }
    }
} 