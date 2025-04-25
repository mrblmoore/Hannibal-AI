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
using HannibalAI.Utils;

namespace HannibalAI.Patches
{
    [HarmonyPatch(typeof(Mission), "OnTick")]
    public class BattleUpdatePatch
    {
        private static bool _battleStarted;
        private static string _currentCommanderId;
        private static readonly AIService _aiService = new AIService("http://localhost:5000", "api/v1");
        private static readonly FallbackService _fallbackService;
        private static BattleController _battleController;
        private static AICommander _commander;
        private static AIDecision _lastDecision;
        private static float _lastUpdateTime;
        private static readonly float UPDATE_INTERVAL = 1.0f;

        static BattleUpdatePatch()
        {
            _fallbackService = new FallbackService(Mission.Current);
        }

        [HarmonyPrefix]
        public static void Prefix(Mission __instance)
        {
            try
            {
                if (!_battleStarted)
                {
                    _battleStarted = true;
                    _currentCommanderId = Guid.NewGuid().ToString();
                    _commander = new AICommander(__instance);
                    _battleController = new BattleController(__instance);
                }

                // Create battle snapshot
                var snapshot = BattleSnapshot.CreateFromMission(__instance, _currentCommanderId);
                if (snapshot == null) return;

                // TODO: Implement battle processing logic using valid Bannerlord APIs
                Debug.Print($"Battle update: {snapshot.BattleId} - {snapshot.State}");
            }
            catch (Exception ex)
            {
                Debug.Print($"Error in battle update: {ex.Message}");
            }
        }

        public static void Postfix(Mission __instance)
        {
            if (__instance?.CurrentState == Mission.State.Continuing)
            {
                var currentTime = __instance.CurrentTime;
                if (currentTime - _lastUpdateTime >= UPDATE_INTERVAL)
                {
                    _lastUpdateTime = currentTime;
                    UpdateBattle(__instance);
                }
            }
        }

        private static void UpdateBattle(Mission mission)
        {
            if (_battleController == null)
            {
                // Initialize controller if needed
                var aiService = new AIService(ModConfig.Instance.AIEndpoint, ModConfig.Instance.APIKey);
                var fallbackService = new FallbackService();
                var commander = new AICommander(1, "AI Commander", 1.0f);
                _battleController = new BattleController(commander, aiService, fallbackService);
            }

            var snapshot = BattleSnapshot.CreateFromMission(mission, _currentCommanderId);
            _battleController.Update(snapshot);
        }

        [HarmonyPatch("OnMissionEnd")]
        [HarmonyPostfix]
        public static void OnMissionEndPostfix()
        {
            _battleController = null;
            _lastUpdateTime = 0f;
            _battleStarted = false;
            _currentCommanderId = "";
        }

        private static async Task UpdateAI(Mission mission)
        {
            if (mission == null)
            {
                return;
            }

            try
            {
                var snapshot = BattleSnapshot.CreateFromMission(mission, _currentCommanderId);
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
            _currentCommanderId = "";
        }

        private static void LogError(string message)
        {
            Logger.LogError(message);
        }

        private static void LogInfo(string message)
        {
            Logger.LogInfo(message);
        }
    }
} 