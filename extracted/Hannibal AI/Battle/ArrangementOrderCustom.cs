using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class ArrangementOrderCustom
    {
        public string OrderType { get; set; }
        public Vec3 Position { get; set; }
        public Vec3 Direction { get; set; }
        public float Width { get; set; }
        public float Depth { get; set; }

        public ArrangementOrderCustom(string orderType, Vec3 position, Vec3 direction, float width, float depth)
        {
            OrderType = orderType;
            Position = position;
            Direction = direction;
            Width = width;
            Depth = depth;
        }
    }
} 