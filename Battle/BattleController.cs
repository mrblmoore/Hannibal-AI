using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly Mission _mission;
        private readonly Dictionary<string, Formation> _formations;

        public BattleController(AICommander commander, AIService aiService, FallbackService fallbackService, Mission mission)
        {
            _commander = commander;
            _aiService = aiService;
            _fallbackService = fallbackService;
            _mission = mission;
            _formations = new Dictionary<string, Formation>();
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

        public void ExecuteCommand(AICommand command)
        {
            if (command == null) return;

            try
            {
                switch (command)
                {
                    case ChangeFormationCommand cmd:
                        HandleChangeFormation(cmd);
                        break;
                    case FlankCommand cmd:
                        HandleFlank(cmd);
                        break;
                    case HoldCommand cmd:
                        HandleHold(cmd);
                        break;
                    case ChargeCommand cmd:
                        HandleCharge(cmd);
                        break;
                    case FollowCommand cmd:
                        HandleFollow(cmd);
                        break;
                    default:
                        Debug.Print($"Unhandled command type: {command.GetType().Name}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Error executing command: {ex.Message}");
            }
        }

        private void HandleChangeFormation(ChangeFormationCommand command)
        {
            if (command.Formation == null) return;
            
            // TODO: Implement formation change using valid Bannerlord APIs
            Debug.Print($"Changing formation {command.Formation.Index}");
        }

        private void HandleFlank(FlankCommand command)
        {
            if (command.Formation == null) return;
            
            // TODO: Implement flanking using valid Bannerlord APIs
            Debug.Print($"Flanking with formation {command.Formation.Index}");
        }

        private void HandleHold(HoldCommand command)
        {
            if (command.Formation == null) return;
            
            // TODO: Implement hold position using valid Bannerlord APIs
            Debug.Print($"Holding position with formation {command.Formation.Index}");
        }

        private void HandleCharge(ChargeCommand command)
        {
            if (command.Formation == null) return;
            
            // TODO: Implement charge using valid Bannerlord APIs
            Debug.Print($"Charging with formation {command.Formation.Index}");
        }

        private void HandleFollow(FollowCommand command)
        {
            if (command.Formation == null) return;
            
            // TODO: Implement follow using valid Bannerlord APIs
            Debug.Print($"Following with formation {command.Formation.Index}");
        }

        private void HandleFallback()
        {
            var fallbackDecision = GetFallbackDecision(_lastSnapshot);
            if (fallbackDecision != null)
            {
                ExecuteDecision(fallbackDecision);
            }
        }

        private AIDecision GetFallbackDecision(BattleSnapshot snapshot)
        {
            return _fallbackService.GetDecisionSync(snapshot);
        }
    }
} 