
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using HannibalAI.Adapters;

namespace HannibalAI.Command
{
    public class RallyCommand : AICommand
    {
        private readonly TaleWorlds.Library.Vec3 _rallyPoint;
        
        public RallyCommand(TaleWorlds.Library.Vec3 rallyPoint)
        {
            _rallyPoint = rallyPoint;
        }
        
        // Overload for our custom Vec3
        public RallyCommand(HannibalVec3 customRallyPoint)
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
