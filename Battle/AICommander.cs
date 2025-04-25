using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using HannibalAI.Command;
using System;

namespace HannibalAI.Battle
{
    public class AICommander
    {
        private readonly FallbackService _fallbackService;

        public AICommander(FallbackService fallbackService)
        {
            _fallbackService = fallbackService ?? throw new ArgumentNullException(nameof(fallbackService));
        }

        public void MakeDecision(Formation formation, Mission mission)
        {
            if (formation == null || mission == null)
                return;

            var snapshot = new BattleSnapshot(mission, formation.Team?.Side.ToString() ?? "Unknown");
            var decision = _fallbackService.GetFallbackDecision(snapshot);

            ExecuteCommand(decision, formation);
        }

        private void ExecuteCommand(AICommand command, Formation formation)
        {
            switch (command)
            {
                case MoveFormationCommand moveCommand:
                    if (formation != null)
                        formation.SetMovementOrder(MovementOrder.MovementOrderMove(moveCommand.Position));
                    break;

                case ChangeFormationCommand changeCommand:
                    if (formation != null)
                        formation.SetFormOrder(changeCommand.FormOrder);
                    break;

                case FlankCommand flankCommand:
                    if (formation != null)
                        formation.FacingOrder = FacingOrder.FacingOrderLookAtDirection(flankCommand.Direction);
                    break;

                case HoldCommand holdCommand:
                    if (formation != null)
                        formation.SetMovementOrder(MovementOrder.MovementOrderMove(holdCommand.Position));
                    break;

                case ChargeCommand chargeCommand:
                    if (formation != null)
                        formation.SetMovementOrder(MovementOrder.MovementOrderCharge());
                    break;

                case FollowCommand followCommand:
                    if (formation != null && followCommand.Target != null)
                        formation.SetMovementOrder(MovementOrder.MovementOrderFollowEntity(followCommand.Target));
                    break;

                default:
                    // If command type is not recognized
                    InformationManager.DisplayMessage(new InformationMessage("Unknown command received."));
                    break;
            }
        }
    }
}
