using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace HannibalAI
{
    /// <summary>
    /// Executes formation orders in the game
    /// </summary>
    public class CommandExecutor
    {
        private static CommandExecutor _instance;
        
        public static CommandExecutor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CommandExecutor();
                }
                return _instance;
            }
        }
        
        private CommandExecutor()
        {
            // Private constructor to enforce singleton
        }
        
        /// <summary>
        /// Execute a formation order with optional aggression factor
        /// Returns true if the order was successfully executed, false otherwise
        /// </summary>
        public bool ExecuteOrder(FormationOrder order, float aggressionFactor = 0.5f)
        {
            if (order == null)
            {
                Logger.Instance.Error("Cannot execute null order");
                System.Diagnostics.Debug.Print("[HannibalAI] ExecuteOrder received null order");
                return false;
            }
            
            if (order.TargetFormation == null)
            {
                Logger.Instance.Error($"Order {order.OrderType} has null target formation");
                System.Diagnostics.Debug.Print($"[HannibalAI] ExecuteOrder: {order.OrderType} has null target formation");
                return false;
            }
            
            if (Mission.Current == null)
            {
                Logger.Instance.Error("Cannot execute order: No active mission");
                System.Diagnostics.Debug.Print("[HannibalAI] ExecuteOrder: No active mission");
                return false;
            }
            
            // Additional validation for formation
            if (order.TargetFormation.CountOfUnits <= 0)
            {
                Logger.Instance.Warning($"Skipping {order.OrderType} order for empty formation {order.TargetFormation.FormationIndex}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Skipping {order.OrderType} for empty formation {order.TargetFormation.FormationIndex}");
                return false;
            }
            
            try
            {
                // Log detailed diagnostic info
                System.Diagnostics.Debug.Print($"[HannibalAI] Executing {order.OrderType} order for formation {order.TargetFormation.FormationIndex} with {order.TargetFormation.CountOfUnits} units");
                
                // Execute the appropriate order based on type
                switch (order.OrderType)
                {
                    case FormationOrderType.Move:
                        return ExecuteMoveOrder(order);
                    case FormationOrderType.Advance:
                        return ExecuteAdvanceOrder(order);
                    case FormationOrderType.Charge:
                        return ExecuteChargeOrder(order);
                    case FormationOrderType.Retreat:
                        return ExecuteRetreatOrder(order);
                    case FormationOrderType.FireAt:
                        return ExecuteFireAtOrder(order);
                    case FormationOrderType.FormLine:
                        return ExecuteFormLineOrder(order);
                    case FormationOrderType.FormCircle:
                        return ExecuteFormCircleOrder(order);
                    case FormationOrderType.FormWedge:
                        return ExecuteFormWedgeOrder(order);
                    case FormationOrderType.FormColumn:
                        return ExecuteFormColumnOrder(order);
                    case FormationOrderType.FormShieldWall:
                        return ExecuteFormShieldWallOrder(order);
                    case FormationOrderType.FormLoose:
                        return ExecuteFormLooseOrder(order);
                    default:
                        Logger.Instance.Error($"Order type {order.OrderType} not implemented");
                        System.Diagnostics.Debug.Print($"[HannibalAI] Unimplemented order type: {order.OrderType}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                // Enhanced error logging with detailed order info and stack trace
                string errorMsg = $"Error executing {order.OrderType} order for formation {order.TargetFormation.FormationIndex}: {ex.Message}";
                Logger.Instance.Error($"{errorMsg}\n{ex.StackTrace}");
                
                // Detailed debug prints for diagnostics
                System.Diagnostics.Debug.Print($"[HannibalAI] ERROR: {errorMsg}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Stack trace: {ex.StackTrace}");
                
                // Get order details for debugging
                string orderDetails = $"Order details: Type={order.OrderType}";
                if (order.TargetPosition != null)
                {
                    orderDetails += $", Position=({order.TargetPosition.x:F1}, {order.TargetPosition.y:F1}, {order.TargetPosition.z:F1})";
                }
                System.Diagnostics.Debug.Print($"[HannibalAI] {orderDetails}");
                
                // Display error message to player if debug is enabled
                if (ModConfig.Instance.Debug)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"HannibalAI order error: {ex.GetType().Name} in {order.OrderType}", 
                        Color.FromUint(0xFF3333U)));
                }
                
                return false;
            }
        }
        
        /// <summary>
        /// Execute a move order
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ExecuteMoveOrder(FormationOrder order)
        {
            try
            {
                // Detailed entry point logging
                System.Diagnostics.Debug.Print($"[HannibalAI] ExecuteMoveOrder entry point for formation {order.TargetFormation.FormationIndex}");
                
                Formation formation = order.TargetFormation;
                Vec3 position = order.TargetPosition;
                
                // Enhanced position validation with detailed diagnostics
                if (position == Vec3.Zero)
                {
                    string warning = "Move order has zero/invalid target position";
                    Logger.Instance.Warning(warning);
                    System.Diagnostics.Debug.Print($"[HannibalAI] ERROR: {warning}");
                    
                    // Show warning in game for debugging
                    if (ModConfig.Instance.Debug)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"HannibalAI: {warning} for {formation.FormationIndex}", 
                            Color.FromUint(0xFFAA00U)));
                    }
                    
                    return false;
                }
                
                // Check formation validity
                if (formation.CountOfUnits <= 0)
                {
                    string warning = $"Formation {formation.FormationIndex} has no units";
                    System.Diagnostics.Debug.Print($"[HannibalAI] WARNING: {warning}");
                    return false;
                }
                
                // Log the operation with more details
                string moveMessage = $"Moving formation {formation.FormationIndex} ({formation.CountOfUnits} units) " + 
                    $"to ({position.x:F1}, {position.y:F1}, {position.z:F1})";
                Logger.Instance.Info(moveMessage);
                System.Diagnostics.Debug.Print($"[HannibalAI] {moveMessage}");
                
                // Skip distance calculation as it's causing compatibility issues
                // We'll implement a simpler position logging approach
                
                System.Diagnostics.Debug.Print($"[HannibalAI] Target position: X={position.x:F1}, Y={position.y:F1}, Z={position.z:F1}");
                
                // Log current formation details
                System.Diagnostics.Debug.Print($"[HannibalAI] Formation details: Index={formation.FormationIndex}, " +
                    $"Units={formation.CountOfUnits}, Team={formation.Team?.TeamIndex ?? -1}");
                
                // Add information about the formation's team and arrangement
                System.Diagnostics.Debug.Print($"[HannibalAI] Formation is controlled by team {formation.Team?.TeamIndex ?? -1}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Formation arrangement: {formation.ArrangementOrder}");
                
                // Log if the position seems out of normal game bounds (probably calculation errors)
                bool positionSeemsValid = Math.Abs(position.x) < 1000 && Math.Abs(position.y) < 1000 && Math.Abs(position.z) < 200;
                if (!positionSeemsValid)
                {
                    string warning = "Target position appears to be outside normal game bounds";
                    System.Diagnostics.Debug.Print($"[HannibalAI] WARNING: {warning}");
                    Logger.Instance.Warning(warning);
                }
                
                // Execute the move order
                WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, position);
                System.Diagnostics.Debug.Print($"[HannibalAI] Executing SetMovementOrder");
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                System.Diagnostics.Debug.Print($"[HannibalAI] SetMovementOrder completed successfully");
                
                // Check if additional data contains formation type instruction
                if (!string.IsNullOrEmpty(order.AdditionalData))
                {
                    System.Diagnostics.Debug.Print($"[HannibalAI] Applying formation arrangement: {order.AdditionalData}");
                    ApplyFormationArrangement(formation, order.AdditionalData);
                    Logger.Instance.Info($"Applied formation arrangement: {order.AdditionalData}");
                }
                
                // Show in-game confirmation in debug mode
                if (ModConfig.Instance.Debug)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Moving {formation.FormationIndex} formation to new position", 
                        Color.FromUint(0x00FF00U)));
                }
                
                return true;
            }
            catch (Exception ex)
            {
                // Enhanced error logging with detailed move info
                string errorMsg = $"Error executing move order for formation {order.TargetFormation.FormationIndex}: {ex.Message}";
                Logger.Instance.Error($"{errorMsg}\n{ex.StackTrace}");
                
                // Detailed debug prints for diagnostics
                System.Diagnostics.Debug.Print($"[HannibalAI] ERROR: {errorMsg}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Stack trace: {ex.StackTrace}");
                
                // Try to determine common error causes
                if (ex.Message.Contains("NullReference"))
                {
                    System.Diagnostics.Debug.Print("[HannibalAI] Possible cause: Null formation or position reference");
                }
                else if (ex.Message.Contains("Scene"))
                {
                    System.Diagnostics.Debug.Print("[HannibalAI] Possible cause: Invalid scene reference");
                }
                
                // Display error message to player if debug is enabled
                if (ModConfig.Instance.Debug)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Move order error: {ex.GetType().Name}", 
                        Color.FromUint(0xFF3333U)));
                }
                
                return false;
            }
        }
        
        /// <summary>
        /// Execute an advance order
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ExecuteAdvanceOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                
                // Log the advance order
                Logger.Instance.Info($"Ordering formation {formation.FormationIndex} to advance");
                System.Diagnostics.Debug.Print($"[HannibalAI] Setting formation {formation.FormationIndex} to advance");
                
                // Use the advance movement order
                formation.SetMovementOrder(MovementOrder.MovementOrderAdvance);
                
                // If there's a target position, log it for informational purposes
                if (order.TargetPosition != Vec3.Zero)
                {
                    Vec3 position = order.TargetPosition;
                    Logger.Instance.Info($"Advance direction toward ({position.x:F1}, {position.y:F1}, {position.z:F1})");
                }
                
                // Apply formation type if specified
                if (!string.IsNullOrEmpty(order.AdditionalData))
                {
                    ApplyFormationArrangement(formation, order.AdditionalData);
                    Logger.Instance.Info($"Applied formation arrangement: {order.AdditionalData}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing advance order: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error in ExecuteAdvanceOrder: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Execute a charge order
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ExecuteChargeOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                
                // Log the charge order
                Logger.Instance.Info($"Ordering formation {formation.FormationIndex} to charge");
                System.Diagnostics.Debug.Print($"[HannibalAI] Setting formation {formation.FormationIndex} to charge");
                
                // Standard charge command
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing charge order: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error in ExecuteChargeOrder: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Execute a retreat order
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ExecuteRetreatOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                
                // Log the retreat order
                Logger.Instance.Info($"Ordering formation {formation.FormationIndex} to retreat");
                System.Diagnostics.Debug.Print($"[HannibalAI] Setting formation {formation.FormationIndex} to retreat");
                
                // Standard retreat command
                formation.SetMovementOrder(MovementOrder.MovementOrderRetreat);
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing retreat order: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error in ExecuteRetreatOrder: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Execute a fire at order for ranged units
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ExecuteFireAtOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                Vec3 targetPosition = order.TargetPosition;
                
                // Validate target position
                if (targetPosition == Vec3.Zero)
                {
                    Logger.Instance.Warning("Fire at order has invalid target position");
                    System.Diagnostics.Debug.Print("[HannibalAI] Fire at order has zero target position");
                    return false;
                }
                
                // Log the fire at order
                Logger.Instance.Info($"Ordering formation {formation.FormationIndex} to fire at position ({targetPosition.x:F1}, {targetPosition.y:F1}, {targetPosition.z:F1})");
                System.Diagnostics.Debug.Print($"[HannibalAI] Setting formation {formation.FormationIndex} to fire at position");
                
                // Set the target position by moving slightly toward it
                Vec2 formationPos = formation.CurrentPosition;
                Vec3 formationPos3D = new Vec3(formationPos.X, formationPos.Y, 0);
                Vec3 direction = targetPosition - formationPos3D;
                direction.Normalize();
                
                Vec3 newPos = formationPos3D + direction * 5f;
                WorldPosition formationWorldPosition = new WorldPosition(Mission.Current.Scene, newPos);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(formationWorldPosition));
                
                // Apply loose formation for archers if not specified otherwise
                if (string.IsNullOrEmpty(order.AdditionalData))
                {
                    ApplyFormationArrangement(formation, "Loose");
                    Logger.Instance.Info("Applied loose formation for archers");
                }
                else
                {
                    ApplyFormationArrangement(formation, order.AdditionalData);
                    Logger.Instance.Info($"Applied {order.AdditionalData} formation");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing fire at order: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error in ExecuteFireAtOrder: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Execute a form line order
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ExecuteFormLineOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                
                // Log the form line order
                Logger.Instance.Info($"Setting formation {formation.FormationIndex} to line arrangement");
                System.Diagnostics.Debug.Print($"[HannibalAI] Setting formation {formation.FormationIndex} to line arrangement");
                
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    Vec3 position = order.TargetPosition;
                    Logger.Instance.Info($"Moving formation to ({position.x:F1}, {position.y:F1}, {position.z:F1})");
                    
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing form line order: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error in ExecuteFormLineOrder: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Execute a form circle order
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ExecuteFormCircleOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                
                // Log the form circle order
                Logger.Instance.Info($"Setting formation {formation.FormationIndex} to circle arrangement");
                System.Diagnostics.Debug.Print($"[HannibalAI] Setting formation {formation.FormationIndex} to circle arrangement");
                
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderCircle;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    Vec3 position = order.TargetPosition;
                    Logger.Instance.Info($"Moving formation to ({position.x:F1}, {position.y:F1}, {position.z:F1})");
                    
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing form circle order: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error in ExecuteFormCircleOrder: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Execute a form wedge order
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ExecuteFormWedgeOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                
                // Log the form wedge order
                Logger.Instance.Info($"Setting formation {formation.FormationIndex} to wedge/skein arrangement");
                System.Diagnostics.Debug.Print($"[HannibalAI] Setting formation {formation.FormationIndex} to wedge/skein arrangement");
                
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderSkein;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    Vec3 position = order.TargetPosition;
                    Logger.Instance.Info($"Moving formation to ({position.x:F1}, {position.y:F1}, {position.z:F1})");
                    
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing form wedge order: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error in ExecuteFormWedgeOrder: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Execute a form column order
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ExecuteFormColumnOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                
                // Log the form column order
                Logger.Instance.Info($"Setting formation {formation.FormationIndex} to column arrangement");
                System.Diagnostics.Debug.Print($"[HannibalAI] Setting formation {formation.FormationIndex} to column arrangement");
                
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderColumn;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    Vec3 position = order.TargetPosition;
                    Logger.Instance.Info($"Moving formation to ({position.x:F1}, {position.y:F1}, {position.z:F1})");
                    
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing form column order: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error in ExecuteFormColumnOrder: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Execute a form shield wall order
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ExecuteFormShieldWallOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                
                // Log the form shield wall order
                Logger.Instance.Info($"Setting formation {formation.FormationIndex} to shield wall");
                System.Diagnostics.Debug.Print($"[HannibalAI] Setting formation {formation.FormationIndex} to shield wall");
                
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                formation.FormOrder = FormOrder.FormOrderWide;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    Vec3 position = order.TargetPosition;
                    Logger.Instance.Info($"Moving formation to ({position.x:F1}, {position.y:F1}, {position.z:F1})");
                    
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing form shield wall order: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error in ExecuteFormShieldWallOrder: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Execute a form loose order
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        private bool ExecuteFormLooseOrder(FormationOrder order)
        {
            try
            {
                Formation formation = order.TargetFormation;
                
                // Log the form loose order
                Logger.Instance.Info($"Setting formation {formation.FormationIndex} to loose arrangement");
                System.Diagnostics.Debug.Print($"[HannibalAI] Setting formation {formation.FormationIndex} to loose arrangement");
                
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
                
                // If position is specified, also move to that position
                if (order.TargetPosition != Vec3.Zero)
                {
                    Vec3 position = order.TargetPosition;
                    Logger.Instance.Info($"Moving formation to ({position.x:F1}, {position.y:F1}, {position.z:F1})");
                    
                    WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, order.TargetPosition);
                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error executing form loose order: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error in ExecuteFormLooseOrder: {ex.Message}");
                return false;
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
                    formation.ArrangementOrder = ArrangementOrder.ArrangementOrderSkein;
                    break;
                case "column":
                    formation.ArrangementOrder = ArrangementOrder.ArrangementOrderColumn;
                    break;
                case "shieldwall":
                    formation.ArrangementOrder = ArrangementOrder.ArrangementOrderShieldWall;
                    break;
                case "loose":
                    formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
                    break;
                case "scatter":
                    formation.ArrangementOrder = ArrangementOrder.ArrangementOrderScatter;
                    break;
                case "square":
                    formation.ArrangementOrder = ArrangementOrder.ArrangementOrderSquare;
                    break;
                case "wide":
                    formation.FormOrder = FormOrder.FormOrderWide;
                    break;
                case "wider":
                    formation.FormOrder = FormOrder.FormOrderWider;
                    break;
                case "deep":
                    formation.FormOrder = FormOrder.FormOrderDeep;
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
        public TaleWorlds.Library.Vec3 TargetPosition { get; set; }
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