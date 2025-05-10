
using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace HannibalAI.Command
{
    /// <summary>
    /// Command to retreat a formation to a safer position
    /// </summary>
    public class RetreatCommand : AICommand
    {
        /// <summary>
        /// Optional target position for the retreat
        /// </summary>
        public TaleWorlds.Library.Vec3 RetreatPosition { get; set; }
        
        /// <summary>
        /// Whether this is an emergency retreat
        /// </summary>
        public bool IsEmergency { get; set; }
        
        public override void Execute(Formation formation)
        {
            if (formation == null)
            {
                System.Diagnostics.Debug.Print("[HannibalAI] ERROR: Null formation in RetreatCommand.Execute");
                return;
            }
            
            try
            {
                // Debug information
                System.Diagnostics.Debug.Print($"[HannibalAI] Executing retreat command for formation {formation.Index}");
                
                // Record detailed formation state before retreat
                Dictionary<string, object> formationState = new Dictionary<string, object>
                {
                    { "FormationClass", formation.FormationIndex },
                    { "UnitCount", formation.CountOfUnits },
                    { "Team", formation.Team?.TeamIndex ?? -1 },
                    { "IsEmergency", IsEmergency }
                };
                Logger.Instance.DiagnosticInfo("RetreatCommand", formationState);
                
                // Get fallback order from service
                var fallbackOrder = FallbackService.Instance.GetFallbackOrder(formation);
                
                // If we have a specific retreat position, use it
                if (HannibalAI.Command.VectorUtility.VecNotEquals(RetreatPosition, Vec3.Zero))
                {
                    System.Diagnostics.Debug.Print($"[HannibalAI] Retreating to position: ({RetreatPosition.x:F1}, {RetreatPosition.y:F1}, {RetreatPosition.z:F1})");
                    
                    // Create retreat order to specified position
                    fallbackOrder = new FormationOrder
                    {
                        OrderType = FormationOrderType.Retreat,
                        TargetFormation = formation,
                        TargetPosition = RetreatPosition
                    };
                }
                
                if (fallbackOrder != null)
                {
                    // Apply the order through CommandUtility
                    System.Diagnostics.Debug.Print($"[HannibalAI] Executing {fallbackOrder.OrderType} order for retreat");
                    CommandUtility.ApplyOrderToFormation(formation, fallbackOrder);
                    
                    // Show visual feedback
                    if (ModConfig.Instance.Debug)
                    {
                        string message = IsEmergency 
                            ? $"EMERGENCY RETREAT for {formation.FormationIndex}" 
                            : $"Retreating {formation.FormationIndex} formation";
                            
                        InformationManager.DisplayMessage(new InformationMessage(
                            message, Color.FromUint(IsEmergency ? 0xFF0000U : 0xFF9900U)));
                    }
                    
                    // Log success
                    string logMessage = $"Retreat order executed for formation {formation.Index}";
                    Logger.Instance.Info(logMessage);
                    System.Diagnostics.Debug.Print($"[HannibalAI] {logMessage}");
                }
                else
                {
                    // Log failure to get fallback order
                    string errorMsg = $"Failed to get fallback order for formation {formation.Index}";
                    Logger.Instance.Warning(errorMsg);
                    System.Diagnostics.Debug.Print($"[HannibalAI] WARNING: {errorMsg}");
                    
                    // Show warning in game
                    if (ModConfig.Instance.Debug)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"No retreat path for {formation.FormationIndex}", Color.FromUint(0xFF0000U)));
                    }
                }
            }
            catch (Exception ex)
            {
                // Enhanced error logging
                string errorMsg = $"Error executing retreat command: {ex.Message}";
                Logger.Instance.Error($"{errorMsg}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] ERROR in RetreatCommand: {errorMsg}");
                System.Diagnostics.Debug.Print($"[HannibalAI] {ex.StackTrace}");
                
                // Show error in game
                if (ModConfig.Instance.Debug)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Retreat error: {ex.GetType().Name}", Color.FromUint(0xFF0000U)));
                }
            }
        }
    }
}
