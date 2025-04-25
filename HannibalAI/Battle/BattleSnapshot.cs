using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;

namespace HannibalAI.Battle
{
    public class BattleSnapshot
    {
        public List<UnitSnapshot> Units { get; set; }
        public Vec3 MapSize { get; set; }
        public Scene Scene { get; set; }
        public float Time { get; set; }

        public BattleSnapshot()
        {
            Units = new List<UnitSnapshot>();
        }
    }

    public class UnitSnapshot
    {
        public int UnitId { get; set; }
        public Vec3 Position { get; set; }
        public Vec3 Direction { get; set; }
        public float Health { get; set; }
        public int FormationIndex { get; set; }
        public bool IsPlayerControlled { get; set; }
    }
} 