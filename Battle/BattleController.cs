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
                    switch (command)
                    {
                        case AttackFormationCommand attackCommand:
                            ExecuteAttackCommand(attackCommand);
                            break;
                        case ChangeFormationCommand formationCommand:
                            ExecuteChangeFormationCommand(formationCommand);
                            break;
                        case MoveFormationCommand moveCommand:
                            ExecuteMoveCommand(moveCommand);
                            break;
                    }
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
            var fallbackDecision = _fallbackService.GetFallbackDecision(_lastSnapshot);
            if (fallbackDecision != null)
            {
                ExecuteDecision(fallbackDecision);
            }
        }

        private void ExecuteAttackCommand(AttackFormationCommand command)
        {
            _commander.AttackFormation(command.Formation, command.TargetFormation);
        }

        private void ExecuteChangeFormationCommand(ChangeFormationCommand command)
        {
            _commander.ChangeFormation(command.Formation, command.NewFormation);
        }

        private void ExecuteMoveCommand(MoveFormationCommand command)
        {
            _commander.MoveFormation(command.Formation, command.Position);
        }
    }
} 