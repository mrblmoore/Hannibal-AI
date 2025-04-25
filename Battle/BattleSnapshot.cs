using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using HannibalAI.Command;

namespace HannibalAI.Battle
{
    public class BattleSnapshot
    {
        public float Time { get; set; }
        public string CommanderId { get; set; }
        public List<UnitData> Units { get; set; }
        public List<FormationSnapshot> Formations { get; set; }
        public TerrainData Terrain { get; set; }
        public WeatherData Weather { get; set; }
        public Vec2 MapSize { get; set; }

        public List<UnitData> PlayerUnits => Units?.Where(u => u.Team?.IsPlayerTeam ?? false).ToList() ?? new List<UnitData>();
        public List<UnitData> EnemyUnits => Units?.Where(u => !(u.Team?.IsPlayerTeam ?? true)).ToList() ?? new List<UnitData>();

        public BattleSnapshot()
        {
            Units = new List<UnitData>();
            Formations = new List<FormationSnapshot>();
        }

        public static BattleSnapshot CreateFromMission(Mission mission, string commanderId)
        {
            if (mission == null || mission.Scene == null)
            {
                return null;
            }

            var snapshot = new BattleSnapshot
            {
                Time = Mission.Current.MissionTime,
                CommanderId = commanderId,
                Units = new List<UnitData>(),
                Formations = new List<FormationSnapshot>()
            };

            // Get map size from scene bounds
            Vec3 min, max;
            mission.Scene.GetBoundingBox(out min, out max);
            snapshot.MapSize = new Vec2(max.x - min.x, max.y - min.y);

            // Collect units
            foreach (var agent in mission.Agents)
            {
                if (agent == null || !agent.IsActive()) continue;
                snapshot.Units.Add(new UnitData(agent));
            }

            // Collect formations
            foreach (var team in mission.Teams)
            {
                if (team == null) continue;
                foreach (var formation in team.FormationsIncludingEmpty)
                {
                    if (formation == null) continue;
                    snapshot.Formations.Add(new FormationSnapshot(formation));
                }
            }

            // Get terrain and weather data
            snapshot.Terrain = new TerrainData(mission.Scene);
            snapshot.Weather = new WeatherData(mission);

            return snapshot;
        }
    }

    public class UnitSnapshot
    {
        public int Id { get; set; }
        public Vec3 Position { get; set; }
        public Vec3 Direction { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public int Team { get; set; }
        public int Formation { get; set; }
        public bool IsMount { get; set; }
        public bool IsRider { get; set; }
        public int MountId { get; set; }
        public int RiderId { get; set; }
    }

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
        }
    }

    public class TerrainSnapshot
    {
        public float Height { get; set; }
        public float WaterLevel { get; set; }
        public bool IsWater { get; set; }
    }

    public class WeatherSnapshot
    {
        public float TimeOfDay { get; set; }
        public float Rain { get; set; }
        public float Fog { get; set; }
    }
}