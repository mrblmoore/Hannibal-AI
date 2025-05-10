using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
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

    /// <summary>
    /// Command to charge the enemy
    /// </summary>
    public class ChargeCommand : AICommand
    {
        /// <summary>
        /// Execute the command on the given formation
        /// </summary>
        public override void Execute(Formation formation)
        {
            if (formation == null) return;
            
            var order = new FormationOrder
            {
                OrderType = FormationOrderType.Charge,
                TargetFormation = formation
            };
            
            Logger.Instance.Info($"[HannibalAI] Charge command executed for formation {formation.Index}");
            CommandUtility.ApplyOrderToFormation(formation, order);
        }
    }

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
    // Define static helper class for command utility methods
    public static class CommandUtility
    {
        // Helper method for applying orders
        public static void ApplyOrderToFormation(Formation formation, FormationOrder order)
        {
            // In a real implementation, this would call into the game API
            // Here we log it for development purposes
            if (formation != null && order != null)
            {
                Logger.Instance.Info($"Applied {order.OrderType} order to formation {formation.Index}");
            }
        }
    }
}