using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using HannibalAI.Command;
using HannibalAI.Services;
using HannibalAI.Battle;

namespace HannibalAI.Battle
{
    public class BattleController
    {
        private readonly AICommander _commander;
        private readonly AIService _aiService;
        private readonly FallbackService _fallbackService;
        private BattleSnapshot _lastSnapshot;
        private AIDecision _lastDecision;

        public BattleController(AICommander commander, AIService aiService, FallbackService fallbackService)
        {
            _commander = commander;
            _aiService = aiService;
            _fallbackService = fallbackService;
        }

        public void Update(BattleSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
            _lastDecision = _aiService.GetDecisionSync(snapshot);
            ExecuteDecision(_lastDecision);
        }

        private void ExecuteDecision(AIDecision decision)
        {
            if (decision == null)
            {
                HandleFallback();
                return;
            }

            foreach (var command in decision.Commands)
            {
                try
                {
                    ExecuteCommand(command);
                }
                catch (Exception ex)
                {
                    HandleFallback();
                    break;
                }
            }
        }

        private void HandleFallback()
        {
            var fallbackDecision = GetFallbackDecision(_lastSnapshot);
            if (fallbackDecision != null)
            {
                ExecuteDecision(fallbackDecision);
            }
        }

        private void ExecuteCommand(AICommand command)
        {
            if (command == null) return;

            try
            {
                if (command is AttackFormationCommand attackCommand)
                {
                    ExecuteAttackCommand(attackCommand);
                }
                else if (command is ChangeFormationCommand changeCommand)
                {
                    ExecuteChangeFormationCommand(changeCommand);
                }
                else if (command is MoveFormationCommand moveCommand)
                {
                    ExecuteMoveCommand(moveCommand);
                }
            }
            catch (Exception)
            {
                // Log error if needed
            }
        }

        private void ExecuteAttackCommand(AttackFormationCommand command)
        {
            if (command?.Formation != null && command?.TargetFormation != null)
            {
                command.Formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }
        }

        private void ExecuteChangeFormationCommand(ChangeFormationCommand command)
        {
            if (command?.Formation != null)
            {
                command.Formation.FormOrder = FormOrder.Line;
            }
        }

        private void ExecuteMoveCommand(MoveFormationCommand command)
        {
            if (command?.Formation != null)
            {
                command.Formation.SetMovementOrder(MovementOrder.MovementOrderMove(command.Position));
            }
        }

        private AIDecision GetFallbackDecision(BattleSnapshot snapshot)
        {
            return _fallbackService.GetDecisionSync(snapshot);
        }
    }
} 