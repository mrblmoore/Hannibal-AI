using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace HannibalAI
{
    /// <summary>
    /// Executes AI commands by interfacing with Bannerlord's formation system
    /// </summary>
    public class CommandExecutor
    {
        /// <summary>
        /// Execute a formation order
        /// </summary>
        public void ExecuteOrder(FormationOrder order)
        {
            if (order == null || order.TargetFormation == null)
            {
                return;
            }
            
            switch (order.OrderType)
            {
                case FormationOrderType.Move:
                    ExecuteMoveOrder(order);
                    break;
                case FormationOrderType.Advance:
                    ExecuteAdvanceOrder(order);
                    break;
                case FormationOrderType.Charge:
                    ExecuteChargeOrder(order);
                    break;
                case FormationOrderType.Retreat:
                    ExecuteRetreatOrder(order);
                    break;
                case FormationOrderType.FireAt:
                    ExecuteFireAtOrder(order);
                    break;
                case FormationOrderType.FormLine:
                    ExecuteFormLineOrder(order);
                    break;
                case FormationOrderType.FormCircle:
                    ExecuteFormCircleOrder(order);
                    break;
                case FormationOrderType.FormWedge:
                    ExecuteFormWedgeOrder(order);
                    break;
                case FormationOrderType.FormColumn:
                    ExecuteFormColumnOrder(order);
                    break;
                case FormationOrderType.FormShieldWall:
                    ExecuteFormShieldWallOrder(order);
                    break;
                case FormationOrderType.FormLoose:
                    ExecuteFormLooseOrder(order);
                    break;
                default:
                    // Unrecognized order type
                    break;
            }
        }
        
        /// <summary>
        /// Execute a move order
        /// </summary>
        private void ExecuteMoveOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                Vec3 position = order.TargetPosition;
                
                WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, position);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                
                // Check if additional data contains formation type instruction
                if (!string.IsNullOrEmpty(order.AdditionalData))
                {
                    ApplyFormationArrangement(formation, order.AdditionalData);
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error executing move order: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Execute an advance order
        /// </summary>
        private void ExecuteAdvanceOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                Vec3 position = order.TargetPosition;
                
                WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, position);
                formation.SetMovementOrder(MovementOrder.MovementOrderAdvance(worldPosition.AsVec2));
                
                // Apply formation type if specified
                if (!string.IsNullOrEmpty(order.AdditionalData))
                {
                    ApplyFormationArrangement(formation, order.AdditionalData);
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error executing advance order: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Execute a charge order
        /// </summary>
        private void ExecuteChargeOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                
                // If we have a specific target position, charge toward that direction
                if (order.TargetPosition != Vec3.Zero)
                {
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderCharge(worldPosition.AsVec2));
                }
                else
                {
                    // General charge order
                    formation.SetMovementOrder(MovementOrder.MovementOrderChargeProperty);
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error executing charge order: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Execute a retreat order
        /// </summary>
        private void ExecuteRetreatOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                Vec3 position = order.TargetPosition;
                
                WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, position);
                formation.SetMovementOrder(MovementOrder.MovementOrderRetreat(worldPosition.AsVec2));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error executing retreat order: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Execute a fire at order for ranged units
        /// </summary>
        private void ExecuteFireAtOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                Vec3 targetPosition = order.TargetPosition;
                
                WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, targetPosition);
                formation.SetFireOrder(FireOrder.FireAtWill);
                
                // Set the target position by moving slightly toward it
                Vec3 formationPos = formation.CurrentPosition;
                Vec3 direction = targetPosition - formationPos;
                direction.Normalize();
                
                Vec3 newPos = formationPos + direction * 5f;
                WorldPosition formationWorldPosition = new WorldPosition(Mission.Current.Scene, newPos);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(formationWorldPosition));
                
                // Apply loose formation for archers if not specified otherwise
                if (string.IsNullOrEmpty(order.AdditionalData))
                {
                    ApplyFormationArrangement(formation, "Loose");
                }
                else
                {
                    ApplyFormationArrangement(formation, order.AdditionalData);
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error executing fire at order: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Execute a form line order
        /// </summary>
        private void ExecuteFormLineOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error executing form line order: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Execute a form circle order
        /// </summary>
        private void ExecuteFormCircleOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderCircle;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error executing form circle order: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Execute a form wedge order
        /// </summary>
        private void ExecuteFormWedgeOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderWedge;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error executing form wedge order: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Execute a form column order
        /// </summary>
        private void ExecuteFormColumnOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderColumn;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error executing form column order: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Execute a form shield wall order
        /// </summary>
        private void ExecuteFormShieldWallOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                formation.FormOrder = FormOrder.FormOrderWide;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error executing form shield wall order: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Execute a form loose order
        /// </summary>
        private void ExecuteFormLooseOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                formation.FormOrder = FormOrder.FormOrderLoose;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error executing form loose order: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Apply a formation arrangement based on string identifier
        /// </summary>
        private void ApplyFormationArrangement(Formation formation, string arrangementType)
        {
            switch (arrangementType.ToLower())
            {
                case "line":
                    formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                    break;
                case "circle":
                    formation.ArrangementOrder = ArrangementOrder.ArrangementOrderCircle;
                    break;
                case "wedge":
                    formation.ArrangementOrder = ArrangementOrder.ArrangementOrderWedge;
                    break;
                case "column":
                    formation.ArrangementOrder = ArrangementOrder.ArrangementOrderColumn;
                    break;
                case "shieldwall":
                    formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                    formation.FormOrder = FormOrder.FormOrderWide;
                    break;
                case "loose":
                    formation.FormOrder = FormOrder.FormOrderLoose;
                    break;
                case "close":
                    formation.FormOrder = FormOrder.FormOrderClose;
                    break;
                case "veryclose":
                    formation.FormOrder = FormOrder.FormOrderVeryClose;
                    break;
                case "wide":
                    formation.FormOrder = FormOrder.FormOrderWide;
                    break;
                case "wider":
                    formation.FormOrder = FormOrder.FormOrderWider;
                    break;
                default:
                    // Unknown arrangement type, ignore
                    break;
            }
        }
    }
    
    /// <summary>
    /// Represents an order to be executed on a formation
    /// </summary>
    public class FormationOrder
    {
        public FormationOrderType OrderType { get; set; }
        public Formation TargetFormation { get; set; }
        public Vec3 TargetPosition { get; set; }
        public string AdditionalData { get; set; }
    }
    
    /// <summary>
    /// Types of orders that can be issued to formations
    /// </summary>
    public enum FormationOrderType
    {
        Move,
        Advance,
        Charge,
        Retreat,
        FireAt,
        FormLine,
        FormCircle,
        FormWedge,
        FormColumn,
        FormShieldWall,
        FormLoose
    }
}
