using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class FormationSnapshot
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public Vec2 Position { get; set; }
        public Vec2 Direction { get; set; }
        public float Width { get; set; }
        public float Depth { get; set; }
        public int UnitCount { get; set; }
        public FormationClass FormationClass { get; set; }
        public float Morale { get; set; }
        public float Fatigue { get; set; }
        public bool IsRanged { get; set; }
        public bool IsCavalry { get; set; }
        public bool IsInfantry { get; set; }

        public FormationSnapshot(Formation formation)
        {
            if (formation == null) return;

            Id = formation.Index;
            TeamId = formation.Team?.TeamIndex ?? -1;
            Position = new Vec2(formation.OrderPosition.x, formation.OrderPosition.y);
            Direction = new Vec2(formation.Direction.x, formation.Direction.y);
            Width = formation.UnitSpacing;
            Depth = formation.UnitSpacing; // Use UnitSpacing for both since FileSpacing is not available
            UnitCount = formation.CountOfUnits;
            FormationClass = formation.FormationIndex;
            Morale = formation.Morale;
            Fatigue = formation.Fatigue;
            IsRanged = formation.IsRanged;
            IsCavalry = formation.IsCavalry;
            IsInfantry = formation.IsInfantry;
        }
    }
} 