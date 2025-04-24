using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.DotNet;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Core;

namespace HannibalAI.Command
{
    public class CommandExecutor
    {
        private readonly Mission _mission;
        private readonly List<string> _currentTactics;

        public CommandExecutor(Mission mission)
        {
            _mission = mission;
            _currentTactics = new List<string>();
        }

        public void ExecuteCommand(string command, Formation formation)
        {
            try
            {
                if (formation == null)
                {
                    LogError("Formation is null");
                    return;
                }

                _currentTactics.Add(command);
                if (_currentTactics.Count > 10) // Keep only last 10 tactics
                {
                    _currentTactics.RemoveAt(0);
                }

                switch (command.ToLower())
                {
                    case "flank":
                        ExecuteFlank(formation);
                        break;
                    case "charge":
                        ExecuteCharge(formation);
                        break;
                    case "retreat":
                        ExecuteRetreat(formation);
                        break;
                    case "hold":
                        ExecuteHold(formation);
                        break;
                    case "line":
                        formation.FormOrder = FormOrder.Line;
                        formation.MovementOrder = MovementOrder.Hold;
                        formation.FacingOrder = FacingOrder.LookAtEnemy;
                        break;
                    case "square":
                        formation.FormOrder = FormOrder.Square;
                        formation.MovementOrder = MovementOrder.Hold;
                        formation.FacingOrder = FacingOrder.LookAtEnemy;
                        break;
                    case "shield_wall":
                        formation.FormOrder = FormOrder.ShieldWall;
                        formation.MovementOrder = MovementOrder.Hold;
                        formation.FacingOrder = FacingOrder.LookAtEnemy;
                        break;
                    case "loose":
                        formation.FormOrder = FormOrder.Loose;
                        formation.MovementOrder = MovementOrder.Hold;
                        formation.FacingOrder = FacingOrder.LookAtEnemy;
                        break;
                    case "circle":
                        formation.FormOrder = FormOrder.Circle;
                        formation.MovementOrder = MovementOrder.Hold;
                        formation.FacingOrder = FacingOrder.LookAtEnemy;
                        break;
                    case "wedge":
                        formation.FormOrder = FormOrder.Wedge;
                        formation.MovementOrder = MovementOrder.Hold;
                        formation.FacingOrder = FacingOrder.LookAtEnemy;
                        break;
                    case "skein":
                        formation.FormOrder = FormOrder.Skein;
                        formation.MovementOrder = MovementOrder.Hold;
                        formation.FacingOrder = FacingOrder.LookAtEnemy;
                        break;
                    case "column":
                        formation.FormOrder = FormOrder.Column;
                        formation.MovementOrder = MovementOrder.Hold;
                        formation.FacingOrder = FacingOrder.LookAtEnemy;
                        break;
                    default:
                        LogError($"Unknown command: {command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error executing command {command}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ExecuteFlank(Formation formation)
        {
            var enemyFormation = _mission.Teams.Where(t => t.Side != formation.Team.Side)
                .SelectMany(t => t.FormationsIncludingEmpty)
                .OrderByDescending(f => f.CountOfUnits)
                .FirstOrDefault();

            if (enemyFormation != null)
            {
                var direction = formation.QuerySystem.MedianPosition.AsVec2 - enemyFormation.QuerySystem.MedianPosition.AsVec2;
                var normalizedDir = direction.Normalized();
                var perpDir = new Vec2(-normalizedDir.y, normalizedDir.x); // 90-degree rotation
                var pos = formation.QuerySystem.MedianPosition;
                formation.MovementOrder = MovementOrder.Move(pos);
                formation.AI.IsControlledByAI = true;
            }
        }

        private void ExecuteCharge(Formation formation)
        {
            formation.FormOrder = FormOrder.Wide;
            formation.MovementOrder = MovementOrder.Charge;
            formation.FacingOrder = FacingOrder.LookAtEnemy;
            formation.AI.IsControlledByAI = true;
        }

        private void ExecuteRetreat(Formation formation)
        {
            formation.FormOrder = FormOrder.Loose;
            formation.MovementOrder = MovementOrder.Retreat;
            formation.FacingOrder = FacingOrder.LookAtEnemy;
            formation.AI.IsControlledByAI = true;
        }

        private void ExecuteHold(Formation formation)
        {
            formation.FormOrder = FormOrder.ShieldWall;
            formation.MovementOrder = MovementOrder.Hold;
            formation.FacingOrder = FacingOrder.LookAtEnemy;
            formation.AI.IsControlledByAI = true;
        }

        public string GetCurrentTactics()
        {
            return string.Join(",", _currentTactics);
        }

        private void LogError(string message)
        {
            try
            {
                File.AppendAllText("hannibal_ai_errors.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
} 