using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using HannibalAI.Command;
using HannibalAI.Utils;

namespace HannibalAI.Battle
{
    public class BattleSnapshot
    {
        public string BattleId { get; private set; }
        public string CommanderId { get; private set; }
        public DateTime Timestamp { get; private set; }
        public WeatherData Weather { get; private set; }
        public List<FormationSnapshot> Formations { get; private set; }
        public BattleState State { get; private set; }
        public float Time { get; set; }
        public List<UnitData> Units { get; set; }
        public TerrainData Terrain { get; set; }
        public Vec2 MapSize { get; set; }
        public Team FriendlyTeam { get; set; }
        public Team EnemyTeam { get; set; }

        public List<UnitData> PlayerUnits => Units?.Where(u => u.Team?.IsPlayerTeam ?? false).ToList() ?? new List<UnitData>();
        public List<UnitData> EnemyUnits => Units?.Where(u => !(u.Team?.IsPlayerTeam ?? true)).ToList() ?? new List<UnitData>();

        public BattleSnapshot(string battleId, string commanderId, WeatherData weather, List<FormationSnapshot> formations, BattleState state)
        {
            BattleId = battleId;
            CommanderId = commanderId;
            Timestamp = DateTime.UtcNow;
            Weather = weather;
            Formations = formations;
            State = state;
        }

        public static BattleSnapshot CreateFromMission(Mission mission, string commanderId)
        {
            try
            {
                var weather = new WeatherData(
                    mission.Scene.GetRainDensity(),
                    mission.Scene.GetSnowDensity(),
                    mission.Scene.GetFogDensity()
                );

                var formations = new List<FormationSnapshot>();
                foreach (var formation in mission.Teams)
                {
                    formations.Add(FormationSnapshot.CreateFromFormation(formation));
                }

                return new BattleSnapshot(
                    Guid.NewGuid().ToString(),
                    commanderId,
                    weather,
                    formations,
                    BattleState.InProgress
                );
            }
            catch (Exception ex)
            {
                LogFile.WriteLine($"Error creating battle snapshot: {ex.Message}");
                return null;
            }
        }
    }

    public enum BattleState
    {
        NotStarted,
        InProgress,
        Completed
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

        public static FormationSnapshot CreateFromFormation(Formation formation)
        {
            return new FormationSnapshot(formation);
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