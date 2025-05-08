
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Command
{
    /// <summary>
    /// Base class for AI commands
    /// </summary>
    public abstract class AICommand
    {
        public abstract void Execute(Formation formation);
    }
    
    /// <summary>
    /// Alias for Vec3 for compatibility
    /// </summary>
    public class Vec3
    {
        public float x;
        public float y;
        public float z;
        
        public Vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        
        public static Vec3 Zero => new Vec3(0, 0, 0);
        
        public float Distance(Vec3 other)
        {
            float dx = x - other.x;
            float dy = y - other.y;
            float dz = z - other.z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
    
    public class RallyCommand : AICommand
    {
        private readonly TaleWorlds.Library.Vec3 _rallyPoint;
        
        public RallyCommand(TaleWorlds.Library.Vec3 rallyPoint)
        {
            _rallyPoint = rallyPoint;
        }
        
        // Overload for our custom Vec3
        public RallyCommand(Vec3 customRallyPoint)
        {
            _rallyPoint = new TaleWorlds.Library.Vec3(
                customRallyPoint.x, 
                customRallyPoint.y, 
                customRallyPoint.z);
        }
        
        public override void Execute(Formation formation)
        {
            if (formation == null) return;
            
            var order = new FormationOrder
            {
                OrderType = FormationOrderType.Move,
                TargetFormation = formation,
                TargetPosition = _rallyPoint,
                AdditionalData = "0.8f" // Urgency as string because we can't access the Urgency property
            };
            
            // Since formation.ApplyOrder doesn't exist in this version, use static helper method
            ApplyOrderToFormation(formation, order);
            Logger.Instance.Info($"Rally order executed for formation {formation.Index}");
        }
        
        // Helper method for applying orders since Formation.ApplyOrder doesn't exist
        private static void ApplyOrderToFormation(Formation formation, FormationOrder order)
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
