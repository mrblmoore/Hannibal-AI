using HannibalAI.Command;
using HannibalAI.Utils;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class CommandExecutor
    {
        private readonly Mission _mission;

        public CommandExecutor(Mission mission)
        {
            _mission = mission;
        }

        public void Execute(AICommand command)
        {
            if (command == null)
            {
                Logger.LogError("CommandExecutor: Command is null.");
                return;
            }

            switch (command)
            {
                case MoveFormationCommand moveCommand:
                    ExecuteMoveCommand(moveCommand);
                    break;

                case AttackFormationCommand attackCommand:
                    ExecuteAttackCommand(attackCommand);
                    break;

                case ChangeFormationCommand changeCommand:
                    ExecuteChangeFormationCommand(changeCommand);
                    break;

                case FlankCommand flankCommand:
                    ExecuteFlankCommand(flankCommand);
                    break;

                case HoldCommand holdCommand:
                    ExecuteHoldCommand(holdCommand);
                    break;

                case ChargeCommand chargeCommand:
                    ExecuteChargeCommand(chargeCommand);
                    break;

                case FollowCommand followCommand:
                    ExecuteFollowCommand(followCommand);
                    break;

                default:
                    Logger.LogError($"CommandExecutor: Unknown command type {command.GetType().Name}.");
                    break;
            }
        }

        private void ExecuteMoveCommand(MoveFormationCommand moveCommand)
        {
            if (moveCommand?.Formation == null)
            {
                Logger.LogError("MoveFormationCommand: Formation is null.");
                return;
            }

            moveCommand.Formation.SetMovementOrder(MovementOrder.MovementOrderMove(moveCommand.TargetPosition));
        }

        private void ExecuteAttackCommand(AttackFormationCommand attackCommand)
        {
            if (attackCommand?.Attacker == null || attackCommand?.Target == null)
            {
                Logger.LogError("AttackFormationCommand: Attacker or Target is null.");
                return;
            }

            attackCommand.Attacker.SetMovementOrder(MovementOrder.MovementOrderChargeToTarget(attackCommand.Target));
        }

        private void ExecuteChangeFormationCommand(ChangeFormationCommand changeCommand)
        {
            if (changeCommand?.Formation == null)
            {
                Logger.LogError("ChangeFormationCommand: Formation is null.");
                return;
            }

            changeCommand.Formation.SetFormationOrder(FormOrder.FormOrderLine);
        }

        private void ExecuteFlankCommand(FlankCommand flankCommand)
        {
            if (flankCommand?.Formation == null)
            {
                Logger.LogError("FlankCommand: Formation is null.");
                return;
            }

            flankCommand.Formation.SetMovementOrder(MovementOrder.MovementOrderMove(flankCommand.TargetPosition));
        }

        private void ExecuteHoldCommand(HoldCommand holdCommand)
        {
            if (holdCommand?.Formation == null)
            {
                Logger.LogError("HoldCommand: Formation is null.");
                return;
            }

            holdCommand.Formation.SetMovementOrder(MovementOrder.MovementOrderStop());
        }

        private void ExecuteChargeCommand(ChargeCommand chargeCommand)
        {
            if (chargeCommand?.Formation == null)
            {
                Logger.LogError("ChargeCommand: Formation is null.");
                return;
            }

            chargeCommand.Formation.SetMovementOrder(MovementOrder.MovementOrderCharge());
        }

        private void ExecuteFollowCommand(FollowCommand followCommand)
        {
            if (followCommand?.Formation == null)
            {
                Logger.LogError("FollowCommand: Formation is null.");
                return;
            }

            followCommand.Formation.SetMovementOrder(MovementOrder.MovementOrderFollow());
        }
    }
}
