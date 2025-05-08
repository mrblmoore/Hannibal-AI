
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    public class RallyCommand : AICommand
    {
        private readonly Vec3 _rallyPoint;
        
        public RallyCommand(Vec3 rallyPoint)
        {
            _rallyPoint = rallyPoint;
        }
        
        public override void Execute(Formation formation)
        {
            if (formation == null) return;
            
            var order = new FormationOrder
            {
                OrderType = FormationOrderType.Move,
                TargetFormation = formation,
                TargetPosition = _rallyPoint,
                Urgency = 0.8f
            };
            
            formation.ApplyOrder(order);
            Logger.Instance.Info($"Rally order executed for formation {formation.Index}");
        }
    }
}
