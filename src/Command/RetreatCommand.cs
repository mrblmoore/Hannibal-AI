
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
                formation.ApplyOrder(fallbackOrder);
                Logger.Instance.Info($"Retreat order executed for formation {formation.Index}");
            }
        }
    }
}
