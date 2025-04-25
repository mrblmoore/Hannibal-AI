using System;
using System.Linq;
using System.IO;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;
using HannibalAI.Battle;
using HannibalAI.Config;

namespace HannibalAI.Command
{
    public class CommandExecutor
    {
        private static readonly string LogFile = "hannibal_ai_errors.log";
        private readonly Mission _mission;

        public CommandExecutor(Mission mission)
        {
            _mission = mission;
        }

        public void ExecuteCommand(AICommand command)
        {
            if (command == null)
            {
                Debug.Print("[HannibalAI] Cannot execute null command");
                return;
            }

            try
            {
                switch (command.Type.ToLower())
                {
                    case "movement":
                        ExecuteMovementCommand(command);
                        break;
                    case "formation":
                        ExecuteFormationCommand(command);
                        break;
                    case "targeting":
                        ExecuteTargetingCommand(command);
                        break;
                    default:
                        Debug.Print($"[HannibalAI] Unknown command type: {command.Type}");
                        break;
                }

                // Log successful execution if debug is enabled
                if (ModConfig.Instance.Debug.ShowAIDecisions)
                {
                    LogInfo($"Executed {command.Type} command on formation {command.FormationIndex}");
                }
            }
            catch (Exception e)
            {
                Debug.Print($"[HannibalAI] Error executing command: {e.Message}");
            }
        }

        private void ExecuteMovementCommand(AICommand command)
        {
            // Implementation
        }

        private void ExecuteFormationCommand(AICommand command)
        {
            // Implementation
        }

        private void ExecuteTargetingCommand(AICommand command)
        {
            // Implementation
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