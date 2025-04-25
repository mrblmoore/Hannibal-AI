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
using HannibalAI.Config;
using System.IO;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Core;
using Debug = TaleWorlds.Library.Debug;
using System.Diagnostics;
using TaleWorlds.Engine;
using System.Reflection;

namespace HannibalAI.Patches
{
    [HarmonyPatch(typeof(Mission))]
    public class BattleUpdatePatch
    {
        private static BattleController _battleController;
        private static float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 1.0f; // Update every second
        private static readonly AIService _aiService;
        private static Command.AIDecision _lastDecision;
        private static bool _battleStarted = false;
        private static string _currentCommanderId;
        private static readonly string LogFile = "hannibal_ai_errors.log";

        static BattleUpdatePatch()
        {
            try
            {
                var config = ModConfig.Instance;
                if (config == null)
                {
                    throw new InvalidOperationException("Failed to load mod configuration");
                }

                _aiService = new AIService(config.AIEndpoint, config.APIKey);
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error initializing BattleUpdatePatch: {ex.Message}");
                throw;
            }
        }

        [HarmonyPatch("Tick")]
        [HarmonyPostfix]
        public static void TickPostfix(Mission __instance, float dt)
        {
            if (__instance == null)
            {
                return;
            }

            try
            {
                if (_battleController == null)
                {
                    _battleController = new BattleController(__instance);
                    _lastUpdateTime = 0f;
                }

                _lastUpdateTime += dt;
                if (_lastUpdateTime < UPDATE_INTERVAL)
                {
                    return;
                }

                _lastUpdateTime = 0f;
                if (mission != null && 
                    mission.CurrentState == MissionState.Continuing &&
                    _battleController != null)
                {
                    _battleController.Update(mission);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error in battle update: {ex.Message}");
                LogError($"Error in battle update: {ex.Message}");
            }
        }

        [HarmonyPatch("OnMissionResult")]
        public static void Postfix()
        {
            try
            {
                if (_battleController != null)
                {
                    _battleController.Cleanup();
                    _battleController = null;
                    Debug.Print("[HannibalAI] Battle controller cleaned up");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error cleaning up battle controller: {ex.Message}");
                LogError($"Error cleaning up battle controller: {ex.Message}");
            }
        }

        private static async Task UpdateAI(Mission mission)
        {
            if (mission == null)
            {
                return;
            }

            try
            {
                var snapshot = BattleSnapshot.CreateFromMission(mission);
                if (snapshot == null)
                {
                    Debug.Print("[HannibalAI] Failed to create battle snapshot");
                    return;
                }

                var decision = await _aiService.ProcessBattleSnapshot(snapshot);
                if (decision == null)
                {
                    Debug.Print("[HannibalAI] No decision received from AI service");
                    return;
                }

                _lastDecision = decision;

                foreach (var command in decision.Commands)
                {
                    if (command == null)
                    {
                        continue;
                    }

                    try
                    {
                        var parameters = new List<object>();
                        
                        if (command.Parameters?.Length > 0)
                        {
                            parameters.Add(command.Parameters[0]);
                        }

                        if (command.Parameters?.Length > 1)
                        {
                            parameters.AddRange(command.Parameters.Skip(1));
                        }

                        var formation = mission.PlayerEnemyTeam?.GetFormation((FormationClass)command.FormationIndex);
                        if (formation == null)
                        {
                            Debug.Print($"[HannibalAI] Formation {command.FormationIndex} not found");
                            continue;
                        }

                        switch (command.Type.ToLower())
                        {
                            case "move":
                                formation.SetMovementOrder(MovementOrder.MovementOrderMove(command.TargetPosition));
                                break;
                            case "charge":
                                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                                break;
                            case "retreat":
                                formation.SetMovementOrder(MovementOrder.MovementOrderRetreat);
                                break;
                            case "hold":
                                formation.SetMovementOrder(MovementOrder.MovementOrderStop);
                                break;
                            default:
                                Debug.Print($"[HannibalAI] Unknown command type: {command.Type}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[HannibalAI] Error executing command: {ex.Message}");
                        LogError($"Error executing command: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error in AI update: {ex.Message}");
                LogError($"Error in AI update: {ex.Message}");
            }
        }

        private static void HandleBattleEnd(Mission mission)
        {
            if (mission == null)
            {
                return;
            }

            try
            {
                var outcome = DetermineBattleOutcome(mission);
                LogInfo($"Battle ended. Outcome: {outcome}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error handling battle end: {ex.Message}");
                LogError($"Error handling battle end: {ex.Message}");
            }
        }

        private static bool IsBattleActive(Mission mission)
        {
            if (mission == null)
            {
                return false;
            }

            try
            {
                return mission.IsFieldBattle && 
                       mission.MissionStarted &&
                       mission.PlayerEnemyTeam != null && 
                       mission.PlayerTeam != null &&
                       mission.PlayerEnemyTeam.ActiveAgents.Count > 0 && 
                       mission.PlayerTeam.ActiveAgents.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static string DetermineBattleOutcome(Mission mission)
        {
            if (mission?.PlayerTeam == null || mission?.PlayerEnemyTeam == null)
            {
                return "Unknown";
            }

            try
            {
                if (mission.PlayerTeam.ActiveAgents.Count == 0)
                {
                    return "Defeat";
                }
                if (mission.PlayerEnemyTeam.ActiveAgents.Count == 0)
                {
                    return "Victory";
                }
                return "Ongoing";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static void ResetBattleState()
        {
            _battleStarted = false;
            _lastUpdateTime = 0f;
            _lastDecision = null;
            _currentCommanderId = null;
        }

        private static void LogError(string message)
        {
            try
            {
                InformationManager.DisplayMessage(new InformationMessage($"[HannibalAI] {message}", Colors.Red));
                File.AppendAllText(LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR: {message}\n");
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error logging message: {ex.Message}");
            }
        }

        private static void LogInfo(string message)
        {
            try
            {
                InformationManager.DisplayMessage(new InformationMessage($"[HannibalAI] {message}", Colors.Green));
                File.AppendAllText(LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} INFO: {message}\n");
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error logging message: {ex.Message}");
            }
        }
    }
} 