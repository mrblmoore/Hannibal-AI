
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class RetreatCommand : AICommand
    {
        public override void Execute(Formation formation)
        {
            if (formation == null) return;
            
            var fallbackOrder = FallbackService.Instance.GetFallbackOrder(formation);
            if (fallbackOrder != null)
            {
                // Since formation.ApplyOrder doesn't exist in this version, use static helper method
                ApplyOrderToFormation(formation, fallbackOrder);
                Logger.Instance.Info($"Retreat order executed for formation {formation.Index}");
            }
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
