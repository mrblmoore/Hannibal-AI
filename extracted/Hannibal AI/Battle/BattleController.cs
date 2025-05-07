using System;
using HannibalAI.Command;
using HannibalAI.Services;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Battle
{
    public class BattleController
    {
        private readonly AICommander _aiCommander;
        private readonly FallbackService _fallbackService;

        public BattleController(AICommander aiCommander, FallbackService fallbackService)
        {
            _aiCommander = aiCommander ?? throw new ArgumentNullException(nameof(aiCommander));
            _fallbackService = fallbackService ?? throw new ArgumentNullException(nameof(fallbackService));
        }

        public void ExecuteAIDecision(AIDecision decision)
        {
            if (decision == null)
                return;

            try
            {
                AICommand command = decision.Command;

                if (command is MoveFormationCommand moveCmd)
                {
                    _aiCommander.MoveFormation(moveCmd.Formation, moveCmd.TargetPosition);
                }
                else if (command is ChangeFormationCommand changeCmd)
                {
                    _aiCommander.ChangeFormation(changeCmd.Formation, changeCmd.FormOrder);
                }
                else if (command is FlankCommand flankCmd)
                {
                    _aiCommander.FlankEnemy(flankCmd.Formation, flankCmd.TargetPosition);
                }
                else if (command is HoldCommand holdCmd)
                {
                    _aiCommander.HoldPosition(holdCmd.Formation, holdCmd.HoldPosition);
                }
                else if (command is ChargeCommand chargeCmd)
                {
                    _aiCommander.ChargeFormation(chargeCmd.Formation);
                }
                else if (command is FollowCommand followCmd)
                {
                    _aiCommander.FollowTarget(followCmd.Follower, followCmd.Leader);
                }
                else
                {
                    _fallbackService.GetFallbackDecision();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error executing AI decision: {ex.Message}");
            }
        }
    }
}
