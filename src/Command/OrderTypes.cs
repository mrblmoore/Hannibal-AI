using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI.Command
{
    /// <summary>
    /// Custom order types for HannibalAI orders
    /// Used to add additional order types not in the base game
    /// </summary>
    public enum HannibalOrderType
    {
        None = 0,
        Move = 1,
        Charge = 2,
        Retreat = 3,
        Hold = 4,
        FollowMe = 5,
        StandGround = 6,
        StrategicAdvance = 7,
        StrategicRetreat = 8,
        Flank = 9
    }
    
    /// <summary>
    /// Adapter for formation orders that properly handles type conversions
    /// </summary>
    public class HannibalFormationOrder
    {
        /// <summary>
        /// Order type
        /// </summary>
        public HannibalOrderType OrderType { get; set; }
        
        /// <summary>
        /// Target formation for the order
        /// </summary>
        public Formation TargetFormation { get; set; }
        
        /// <summary>
        /// Target position for the order
        /// </summary>
        public TaleWorlds.Library.Vec3 TargetPosition { get; set; } = TaleWorlds.Library.Vec3.Zero;
        
        /// <summary>
        /// Convert our custom order to TaleWorlds API order
        /// </summary>
        public FormationOrder ToFormationOrder()
        {
            var result = new FormationOrder
            {
                TargetFormation = this.TargetFormation
            };
            
            // Convert our custom order type to TaleWorlds order type
            switch (OrderType)
            {
                case HannibalOrderType.Move:
                    // We know Move is a valid order type (value 1)
                    result.OrderType = (FormationOrderType)1;
                    result.TargetPosition = this.TargetPosition;
                    break;
                    
                case HannibalOrderType.Charge:
                    // We know Charge is a valid order type (value 2) 
                    result.OrderType = (FormationOrderType)2;
                    break;
                    
                case HannibalOrderType.Retreat:
                    // We know Retreat is a valid order type (value 3)
                    result.OrderType = (FormationOrderType)3;
                    result.TargetPosition = this.TargetPosition;
                    break;
                    
                case HannibalOrderType.Hold:
                case HannibalOrderType.StandGround:
                    // Use value 9 for Hold/StandGround (custom value)
                    result.OrderType = (FormationOrderType)9;
                    break;
                    
                case HannibalOrderType.FollowMe:
                    // Use a reasonable value for FollowMe
                    result.OrderType = (FormationOrderType)4; // Assuming 4 = FollowMe
                    break;
                    
                default:
                    // Use value 0 (default/none) as fallback
                    result.OrderType = (FormationOrderType)0;
                    break;
            }
            
            return result;
        }
        
        /// <summary>
        /// Create a move order
        /// </summary>
        public static HannibalFormationOrder CreateMoveOrder(Formation formation, TaleWorlds.Library.Vec3 position)
        {
            return new HannibalFormationOrder
            {
                OrderType = HannibalOrderType.Move,
                TargetFormation = formation,
                TargetPosition = position
            };
        }
        
        /// <summary>
        /// Create a charge order
        /// </summary>
        public static HannibalFormationOrder CreateChargeOrder(Formation formation)
        {
            return new HannibalFormationOrder
            {
                OrderType = HannibalOrderType.Charge,
                TargetFormation = formation
            };
        }
        
        /// <summary>
        /// Create a hold order
        /// </summary>
        public static HannibalFormationOrder CreateHoldOrder(Formation formation, TaleWorlds.Library.Vec3 position = new TaleWorlds.Library.Vec3())
        {
            return new HannibalFormationOrder
            {
                OrderType = HannibalOrderType.Hold,
                TargetFormation = formation,
                TargetPosition = position
            };
        }
        
        /// <summary>
        /// Create a retreat order
        /// </summary>
        public static HannibalFormationOrder CreateRetreatOrder(Formation formation, TaleWorlds.Library.Vec3 position = new TaleWorlds.Library.Vec3())
        {
            return new HannibalFormationOrder
            {
                OrderType = HannibalOrderType.Retreat,
                TargetFormation = formation,
                TargetPosition = position
            };
        }
    }
}