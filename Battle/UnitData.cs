using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;

namespace HannibalAI.Battle
{
    public class UnitData
    {
        public int Id { get; set; }
        public int UnitId { get; set; }
        public Vec3 Position { get; set; }
        public Vec3 Direction { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public Team Team { get; set; }
        public Formation Formation { get; set; }
        public int FormationIndex { get; set; }
        public bool IsMounted { get; set; }
        public bool IsRider { get; set; }
        public bool IsRanged { get; set; }
        public bool IsPlayerControlled { get; set; }

        public UnitData(Agent agent)
        {
            if (agent == null) return;

            Id = agent.Index;
            UnitId = agent.Index;
            Position = agent.Position;
            Direction = agent.LookDirection;
            Health = agent.Health;
            MaxHealth = agent.HealthLimit;
            Team = agent.Team;
            Formation = agent.Formation;
            FormationIndex = agent.Formation?.Index ?? -1;
            IsMounted = agent.HasMount;
            IsRider = agent.MountAgent != null;
            IsRanged = HasRangedWeapon(agent);
            IsPlayerControlled = agent.IsPlayerControlled;
        }

        public bool HasRangedWeapon(Agent agent)
        {
            if (agent?.Equipment == null) return false;

            var mainHandItem = agent.Equipment[EquipmentIndex.Weapon0].Item;
            var offHandItem = agent.Equipment[EquipmentIndex.Weapon1].Item;

            return (mainHandItem?.Type == ItemObject.ItemTypeEnum.Bow || 
                    mainHandItem?.Type == ItemObject.ItemTypeEnum.Crossbow ||
                    mainHandItem?.Type == ItemObject.ItemTypeEnum.Thrown) ||
                   (offHandItem?.Type == ItemObject.ItemTypeEnum.Bow ||
                    offHandItem?.Type == ItemObject.ItemTypeEnum.Crossbow ||
                    offHandItem?.Type == ItemObject.ItemTypeEnum.Thrown);
        }
    }
} 