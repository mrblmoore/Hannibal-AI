using System;
using System.Linq;
using System.IO;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;
using HannibalAI.Battle;

namespace HannibalAI.Command
{
    public class CommandExecutor
    {
        private static readonly string LogFile = "hannibal_ai_errors.log";
        private static readonly BattleController _controller = BattleController.Instance;

        public static void ExecuteCommand(string command, Mission mission, object[] parameters = null)
        {
            try
            {
                var team = mission.PlayerEnemyTeam;
                if (team == null) return;

                // Default to infantry if no formation type specified
                var formationType = parameters?.Length > 0 ? parameters[0].ToString() : "infantry";
                var formation = _controller.GetFormation(team, formationType);

                if (formation == null)
                {
                    LogError($"Could not find formation of type {formationType}");
                    return;
                }

                switch (command.ToLower())
                {
                    case "flank":
                    case "charge":
                    case "retreat":
                    case "hold":
                    case "advance":
                        _controller.ExecuteMovementCommand(formation, command, parameters);
                        break;

                    case "line":
                    case "shield_wall":
                    case "loose":
                    case "circle":
                    case "square":
                    case "column":
                        _controller.ExecuteFormationCommand(formation, command, parameters);
                        break;

                    case "focus_fire":
                    case "hold_fire":
                        _controller.ExecuteTargetingCommand(formation, command, parameters);
                        break;

                    default:
                        LogError($"Unknown command: {command}");
                        break;
                }

                // Log successful execution if debug is enabled
                if (SubModule.GetConfig().Debug)
                {
                    LogInfo($"Executed command {command} on {formationType} formation");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing command {command}: {ex.Message}");
            }
        }

        private static void LogError(string message)
        {
            try
            {
                File.AppendAllText(LogFile, $"[ERROR {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}\n");
                Debug.Print($"[HannibalAI] {message}");
            }
            catch (Exception ex)
            {
                // If we can't log to file, at least write to debug output
                Debug.Print($"Error logging to file: {ex.Message}");
                Debug.Print($"Original error: {message}");
            }
        }

        private static void LogInfo(string message)
        {
            try
            {
                File.AppendAllText(LogFile, $"[INFO {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}\n");
                Debug.Print($"[HannibalAI] {message}");
            }
            catch { /* Ignore logging errors */ }
        }
    }
} 