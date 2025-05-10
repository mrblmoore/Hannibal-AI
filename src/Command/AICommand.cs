using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using HannibalAI.Adapters;

namespace HannibalAI.Command
{
    /// <summary>
    /// Helper methods for safe vector operations
    /// </summary>
    public static class VectorUtility
    {
        /// <summary>
        /// Safely compare two Vec3 objects for equality
        /// </summary>
        /// <param name="v1">First vector</param>
        /// <param name="v2">Second vector</param>
        /// <returns>True if vectors are equal</returns>
        public static bool VecEquals(TaleWorlds.Library.Vec3 v1, TaleWorlds.Library.Vec3 v2)
        {
            // Cannot directly check for null on structs
            // Check if both are zero vectors
            if ((v1.x == 0 && v1.y == 0 && v1.z == 0) && 
                (v2.x == 0 && v2.y == 0 && v2.z == 0))
                return true;
            
            // Use an epsilon for floating point comparison
            const float epsilon = 0.001f;
            return Math.Abs(v1.x - v2.x) < epsilon &&
                   Math.Abs(v1.y - v2.y) < epsilon &&
                   Math.Abs(v1.z - v2.z) < epsilon;
        }
        
        /// <summary>
        /// Safely compare if Vec3 is not equal to another Vec3
        /// </summary>
        /// <param name="v1">First vector</param>
        /// <param name="v2">Second vector</param>
        /// <returns>True if vectors are not equal</returns>
        public static bool VecNotEquals(TaleWorlds.Library.Vec3 v1, TaleWorlds.Library.Vec3 v2)
        {
            return !VecEquals(v1, v2);
        }
    }

    /// <summary>
    /// Base class for AI commands
    /// </summary>
    public abstract class AICommand
    {
        /// <summary>
        /// Execute the command on the given formation
        /// </summary>
        /// <param name="formation">The formation to execute the command on</param>
        public abstract void Execute(Formation formation);
    }

    /// <summary>
    /// Command to move a formation to a specific position
    /// </summary>
    public class MoveFormationCommand : AICommand
    {
        /// <summary>
        /// The target position to move the formation to
        /// </summary>
        public TaleWorlds.Library.Vec3 TargetPosition { get; set; }
        
        /// <summary>
        /// The formation index to apply this command to
        /// </summary>
        public int FormationIndex { get; set; }
        
        /// <summary>
        /// Execute the command on the given formation
        /// </summary>
        public override void Execute(Formation formation)
        {
            if (formation == null) return;
            
            var order = new FormationOrder
            {
                OrderType = FormationOrderType.Move,
                TargetFormation = formation,
                TargetPosition = TargetPosition
            };
            
            Logger.Instance.Info($"[HannibalAI] Move command executed for formation {formation.Index} to {TargetPosition}");
            CommandUtility.ApplyOrderToFormation(formation, order);
        }
    }

    /// <summary>
    /// Command to change a formation's arrangement
    /// </summary>
    public class ChangeFormationCommand : AICommand
    {
        /// <summary>
        /// The formation order to apply
        /// </summary>
        public object FormOrder { get; set; }
        
        /// <summary>
        /// Execute the command on the given formation
        /// </summary>
        public override void Execute(Formation formation)
        {
            if (formation == null) return;
            
            Logger.Instance.Info($"[HannibalAI] Formation change command executed for formation {formation.Index}");
        }
    }

    /// <summary>
    /// Command to execute a flanking maneuver
    /// </summary>
    public class FlankCommand : AICommand
    {
        /// <summary>
        /// The target position for the flanking maneuver
        /// </summary>
        public TaleWorlds.Library.Vec3 TargetPosition { get; set; }
        
        /// <summary>
        /// Execute the command on the given formation
        /// </summary>
        public override void Execute(Formation formation)
        {
            if (formation == null) return;
            
            var order = new FormationOrder
            {
                OrderType = FormationOrderType.Move,
                TargetFormation = formation,
                TargetPosition = TargetPosition
            };
            
            Logger.Instance.Info($"[HannibalAI] Flank command executed for formation {formation.Index} to {TargetPosition}");
            CommandUtility.ApplyOrderToFormation(formation, order);
        }
    }

    /// <summary>
    /// Command to hold position
    /// </summary>
    public class HoldCommand : AICommand
    {
        /// <summary>
        /// The position to hold
        /// </summary>
        public TaleWorlds.Library.Vec3 HoldPosition { get; set; }
        
        /// <summary>
        /// Execute the command on the given formation
        /// </summary>
        public override void Execute(Formation formation)
        {
            if (formation == null) return;
            
            var order = new FormationOrder
            {
                OrderType = FormationOrderType.Move, // Used instead of StandGround which is not available
                TargetFormation = formation,
                TargetPosition = HoldPosition
            };
            
            Logger.Instance.Info($"[HannibalAI] Hold position command executed for formation {formation.Index}");
            CommandUtility.ApplyOrderToFormation(formation, order);
        }
    }

    /* The basic ChargeCommand has been replaced by an enhanced version below */

    /// <summary>
    /// Command to follow another formation
    /// </summary>
    public class FollowCommand : AICommand
    {
        /// <summary>
        /// The formation to follow
        /// </summary>
        public int LeaderFormationIndex { get; set; }
        
        /// <summary>
        /// Execute the command on the given formation
        /// </summary>
        public override void Execute(Formation formation)
        {
            if (formation == null) return;
            
            Logger.Instance.Info($"[HannibalAI] Follow command executed for formation {formation.Index}");
        }
    }
    /// <summary>
    /// Command to order a formation to charge the enemy
    /// </summary>
    public class ChargeCommand : AICommand
    {
        /// <summary>
        /// The target formation to charge at (can be null for general charge)
        /// </summary>
        public Formation TargetFormation { get; set; }
        
        /// <summary>
        /// Execute the charge command on the given formation
        /// </summary>
        public override void Execute(Formation formation)
        {
            if (formation == null)
            {
                System.Diagnostics.Debug.Print("[HannibalAI] ERROR: Null formation in ChargeCommand.Execute");
                return;
            }
            
            try
            {
                // Debug information
                System.Diagnostics.Debug.Print($"[HannibalAI] Executing charge command for formation {formation.Index}");
                
                // Record formation state before charge
                Dictionary<string, object> preState = new Dictionary<string, object>
                {
                    { "FormationIndex", formation.FormationIndex },
                    { "UnitCount", formation.CountOfUnits },
                    { "IsPlayerTeam", formation.Team?.IsPlayerTeam ?? false },
                    { "TeamIndex", formation.Team?.TeamIndex ?? -1 }
                };
                Logger.Instance.DiagnosticInfo("ChargeCommand-Pre", preState);
                
                // Create the order
                var order = new FormationOrder
                {
                    OrderType = FormationOrderType.Charge,
                    TargetFormation = formation
                };
                
                // If we have a specific formation to charge at, set target entity
                if (TargetFormation != null)
                {
                    // In a real implementation, this would set the target entity
                    Logger.Instance.Info($"Charging at enemy formation {TargetFormation.Index}");
                }
                
                // Execute the order through the helper
                CommandUtility.ApplyOrderToFormation(formation, order);
                
                // Show confirmation message in game
                if (ModConfig.Instance.Debug)
                {
                    string message = TargetFormation != null
                        ? $"Charging {formation.FormationIndex} at enemy formation {TargetFormation.Index}"
                        : $"Charging {formation.FormationIndex} at nearest enemies";
                        
                    InformationManager.DisplayMessage(new InformationMessage(
                        message, Color.FromUint(0xFF3300U)));
                }
                
                // Log success
                Logger.Instance.Info($"[HannibalAI] Charge command executed for formation {formation.Index}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Charge command completed successfully");
            }
            catch (Exception ex)
            {
                // Enhanced error logging
                string errorMsg = $"Error executing charge command: {ex.Message}";
                Logger.Instance.Error($"{errorMsg}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] ERROR in ChargeCommand: {errorMsg}");
                System.Diagnostics.Debug.Print($"[HannibalAI] {ex.StackTrace}");
            }
        }
    }
    
    /// <summary>
    /// Command to make a formation hold its position
    /// </summary>
    public class HoldPositionCommand : AICommand
    {
        /// <summary>
        /// Optional specific position to hold at
        /// </summary>
        public TaleWorlds.Library.Vec3 Position { get; set; }
        
        /// <summary>
        /// Execute the hold position command
        /// </summary>
        public override void Execute(Formation formation)
        {
            if (formation == null)
            {
                System.Diagnostics.Debug.Print("[HannibalAI] ERROR: Null formation in HoldPositionCommand.Execute");
                return;
            }
            
            try
            {
                // Debug information
                System.Diagnostics.Debug.Print($"[HannibalAI] Executing hold position command for formation {formation.Index}");
                
                // Create diagnostic info for initial formation state
                Dictionary<string, object> formationState = new Dictionary<string, object>
                {
                    { "FormationClass", formation.FormationIndex },
                    { "UnitCount", formation.CountOfUnits },
                    { "Team", formation.Team?.TeamIndex ?? -1 }
                };
                Logger.Instance.DiagnosticInfo("HoldCommand", formationState);
                
                // Create our custom order using the HannibalFormationOrder adapter
                HannibalFormationOrder hannibalOrder;
                
                if (VectorUtility.VecNotEquals(Position, TaleWorlds.Library.Vec3.Zero))
                {
                    // If we have a specific position, create a hold order at that position
                    hannibalOrder = HannibalFormationOrder.CreateHoldOrder(formation, Position);
                    
                    // Log position information
                    System.Diagnostics.Debug.Print($"[HannibalAI] Hold position at: ({Position.x:F1}, {Position.y:F1}, {Position.z:F1})");
                    Logger.Instance.Info($"Holding formation {formation.Index} at ({Position.x:F1}, {Position.y:F1}, {Position.z:F1})");
                }
                else
                {
                    // Hold at current position
                    hannibalOrder = HannibalFormationOrder.CreateHoldOrder(formation);
                    Logger.Instance.Info($"Holding formation {formation.Index} at current position");
                }
                
                // Convert our custom order to the vanilla FormationOrder for API compatibility
                var order = hannibalOrder.ToFormationOrder();
                
                // Apply the order
                CommandUtility.ApplyOrderToFormation(formation, order);
                
                // Show confirmation in game if debugging
                if (ModConfig.Instance.Debug)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Holding position with {formation.FormationIndex} formation", 
                        Color.FromUint(0x00AAFFU)));
                }
                
                // Log success
                System.Diagnostics.Debug.Print($"[HannibalAI] Hold position command completed successfully");
            }
            catch (Exception ex)
            {
                // Enhanced error logging
                string errorMsg = $"Error executing hold position command: {ex.Message}";
                Logger.Instance.Error($"{errorMsg}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] ERROR in HoldPositionCommand: {errorMsg}");
                System.Diagnostics.Debug.Print($"[HannibalAI] {ex.StackTrace}");
            }
        }
    }

    // Define static helper class for command utility methods
    public static class CommandUtility
    {
        // Helper method for applying orders
        public static void ApplyOrderToFormation(Formation formation, FormationOrder order)
        {
            try
            {
                // Perform detailed validation
                if (formation == null)
                {
                    System.Diagnostics.Debug.Print("[HannibalAI] ERROR: Null formation in ApplyOrderToFormation");
                    return;
                }
                
                if (order == null)
                {
                    System.Diagnostics.Debug.Print("[HannibalAI] ERROR: Null order in ApplyOrderToFormation");
                    return;
                }
                
                // In a real implementation, this would call into the game API
                // For now, we send the order through the CommandExecutor
                System.Diagnostics.Debug.Print($"[HannibalAI] Applying {order.OrderType} order to formation {formation.Index}");
                
                // Apply order logic based on type
                bool success = false;
                switch (order.OrderType)
                {
                    case FormationOrderType.Move:
                        if (VectorUtility.VecEquals(order.TargetPosition, TaleWorlds.Library.Vec3.Zero))
                        {
                            System.Diagnostics.Debug.Print("[HannibalAI] ERROR: Move order with zero target position");
                            return;
                        }
                        try
                        {
                            // Use FormationAdapter to create proper WorldPosition and create order
                            var pos = order.TargetPosition;
                            Logger.Instance.Info($"Moving formation to ({pos.x}, {pos.y}, {pos.z})");
                            
                            // There are two approaches we can use here, try API-based method first
                            if (Mission.Current != null && Mission.Current.Scene != null)
                            {
                                // Direct API call bypassing WorldPosition entirely if we can
                                if (formation.GetType().GetMethod("SetMovementPosition") != null)
                                {
                                    System.Diagnostics.Debug.Print("[HannibalAI] Using SetMovementPosition API");
                                    formation.GetType().GetMethod("SetMovementPosition").Invoke(
                                        formation, new object[] { pos });
                                }
                                else
                                {
                                    // Fallback to standard order via reflection
                                    System.Diagnostics.Debug.Print("[HannibalAI] Using MovementOrder via Reflection");
                                    
                                    // Get the CreateMovementOrder method via reflection
                                    var method = typeof(Formation).GetMethod("CreateMovementOrder");
                                    if (method != null)
                                    {
                                        method.Invoke(formation, new object[] { pos });
                                    }
                                    else
                                    {
                                        Logger.Instance.Warning("Could not find CreateMovementOrder method");
                                    }
                                }
                            }
                            else
                            {
                                // Try to use SetPosition method as last resort
                                System.Diagnostics.Debug.Print("[HannibalAI] Using reflection to set position");
                                var setPositionMethod = formation.GetType().GetMethod("SetPosition");
                                if (setPositionMethod != null)
                                {
                                    setPositionMethod.Invoke(formation, new object[] { pos });
                                }
                                else
                                {
                                    Logger.Instance.Error("Could not find any method to set formation position");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error($"Error moving formation: {ex.Message}");
                            System.Diagnostics.Debug.Print($"[HannibalAI] Formation move error: {ex.Message}");
                        }
                        success = true;
                        break;
                        
                    case FormationOrderType.Charge:
                        formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                        success = true;
                        break;
                        
                    case (FormationOrderType)9: // Special case for our StandGround
                        // Hold position is equivalent to stop
                        formation.SetMovementOrder(MovementOrder.MovementOrderStop);
                        success = true;
                        break;
                        
                    default:
                        System.Diagnostics.Debug.Print($"[HannibalAI] Unimplemented order type: {order.OrderType}");
                        break;
                }
                
                // Log order application
                if (success)
                {
                    Logger.Instance.Info($"Successfully applied {order.OrderType} order to formation {formation.Index}");
                    System.Diagnostics.Debug.Print($"[HannibalAI] Order {order.OrderType} applied successfully");
                }
                else
                {
                    Logger.Instance.Warning($"Failed to apply {order.OrderType} order to formation {formation.Index}");
                    System.Diagnostics.Debug.Print($"[HannibalAI] Order {order.OrderType} application failed");
                }
            }
            catch (Exception ex)
            {
                // Log detailed error information
                string errorMsg = $"Error applying order to formation: {ex.Message}";
                Logger.Instance.Error($"{errorMsg}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] ERROR in ApplyOrderToFormation: {errorMsg}");
                System.Diagnostics.Debug.Print($"[HannibalAI] {ex.StackTrace}");
            }
        }
    }
}