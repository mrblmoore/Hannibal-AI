using System;
using System.IO;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Services
{
    public class FallbackService
    {
        private readonly string _logPath = "hannibal_ai_errors.log";

        public void HandleFallback(Mission mission)
        {
            try
            {
                if (mission?.MainAgent == null || !mission.MainAgent.IsActive())
                {
                    LogFallback("Main agent is null or inactive");
                    return;
                }

                var playerTeam = mission.MainAgent.Team;
                if (playerTeam == null)
                {
                    LogFallback("Player team is null");
                    return;
                }

                var enemyTeam = mission.Teams.FirstOrDefault(t => t.Side != playerTeam.Side);
                if (enemyTeam == null)
                {
                    LogFallback("Enemy team not found");
                    return;
                }

                var playerActiveAgents = playerTeam.ActiveAgents.Count();
                var enemyActiveAgents = enemyTeam.ActiveAgents.Count();

                foreach (var formation in playerTeam.FormationsIncludingEmpty)
                {
                    if (formation == null || formation.CountOfUnits == 0) continue;

                    formation.AI.IsControlledByAI = true;
                    
                    if (playerActiveAgents < enemyActiveAgents * 0.5f)
                    {
                        // Defensive fallback - retreat and regroup
                        formation.MovementOrder = MovementOrder.Retreat;
                        LogFallback($"Defensive retreat for formation {formation.Index}");
                    }
                    else if (playerActiveAgents > enemyActiveAgents * 1.5f)
                    {
                        // Aggressive fallback - charge
                        formation.MovementOrder = MovementOrder.Charge;
                        LogFallback($"Aggressive charge for formation {formation.Index}");
                    }
                    else
                    {
                        // Balanced fallback - hold position
                        formation.MovementOrder = MovementOrder.StandGround;
                        LogFallback($"Holding position for formation {formation.Index}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogFallback($"Error in HandleFallback: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void LogFallback(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
            catch
            {
                // Silently fail if logging fails
            }
        }
    }
} 