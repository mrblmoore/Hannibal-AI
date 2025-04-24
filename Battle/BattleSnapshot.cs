using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace HannibalAI.Battle
{
    public class BattleSnapshot
    {
        public string CommanderId { get; set; }
        public float BattleTime { get; set; }
        public Vec3 BattlefieldCenter { get; set; }
        public float BattlefieldRadius { get; set; }
        public List<UnitData> PlayerUnits { get; set; }
        public List<UnitData> EnemyUnits { get; set; }
        public TerrainData Terrain { get; set; }
        public WeatherData Weather { get; set; }
        public List<FormationData> PlayerFormations { get; set; }
        public List<FormationData> EnemyFormations { get; set; }

        public static BattleSnapshot CreateFromMission(Mission mission)
        {
            try
            {
                if (mission == null || !mission.IsFieldBattle)
                    return null;

                var playerTeam = mission.PlayerTeam;
                var enemyTeam = mission.PlayerEnemyTeam;

                if (playerTeam == null || enemyTeam == null)
                    return null;

                return new BattleSnapshot
                {
                    CommanderId = enemyTeam.Leader?.Id.ToString() ?? "unknown",
                    BattleTime = mission.CurrentTime,
                    BattlefieldCenter = mission.Scene.GetBoundingBox().Center,
                    BattlefieldRadius = mission.Scene.GetBoundingBox().GetMaxDimension() / 2f,
                    PlayerUnits = GetUnitsData(playerTeam),
                    EnemyUnits = GetUnitsData(enemyTeam),
                    Terrain = GetTerrainData(mission),
                    Weather = GetWeatherData(mission),
                    PlayerFormations = GetFormationsData(playerTeam),
                    EnemyFormations = GetFormationsData(enemyTeam)
                };
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error creating battle snapshot: {ex.Message}");
                return null;
            }
        }

        private static List<UnitData> GetUnitsData(Team team)
        {
            return team.ActiveAgents
                .Where(a => a.IsHuman && !a.IsRunningAway)
                .Select(a => new UnitData
                {
                    Id = a.Index,
                    Position = a.Position.ToVec3(),
                    Health = a.Health,
                    MaxHealth = a.HealthLimit,
                    IsRanged = HasRangedWeapon(a),
                    IsMounted = a.IsMount || a.HasMount,
                    FormationIndex = (int)a.Formation?.FormationIndex ?? -1,
                    Class = GetUnitClass(a)
                })
                .ToList();
        }

        private static List<FormationData> GetFormationsData(Team team)
        {
            return team.FormationsIncludingEmpty
                .Where(f => f != null && f.CountOfUnits > 0)
                .Select(f => new FormationData
                {
                    FormationIndex = (int)f.FormationIndex,
                    UnitCount = f.CountOfUnits,
                    Position = f.CurrentPosition,
                    Direction = f.Direction,
                    FormationType = f.FormOrderType.ToString(),
                    Width = f.Width,
                    Depth = f.Depth,
                    IsRanged = f.QuerySystem.IsRangedFormation,
                    IsMounted = f.QuerySystem.IsCavalryFormation
                })
                .ToList();
        }

        private static TerrainData GetTerrainData(Mission mission)
        {
            var scene = mission.Scene;
            return new TerrainData
            {
                HeightMap = GetHeightMapSample(scene),
                HasForest = scene.HasForest(),
                HasHills = scene.GetTerrainHeight(Vec2.Zero) > 1.5f,
                HasWater = scene.HasWater()
            };
        }

        private static WeatherData GetWeatherData(Mission mission)
        {
            return new WeatherData
            {
                TimeOfDay = mission.TimeOfDay,
                IsNight = mission.Scene.IsNight(),
                IsRaining = mission.Scene.IsRaining(),
                IsFoggy = mission.Scene.IsFoggy()
            };
        }

        private static float[,] GetHeightMapSample(Scene scene)
        {
            const int sampleSize = 10;
            var heightMap = new float[sampleSize, sampleSize];
            var bounds = scene.GetBoundingBox();
            var stepX = bounds.Width / (sampleSize - 1);
            var stepY = bounds.Height / (sampleSize - 1);

            for (int x = 0; x < sampleSize; x++)
            {
                for (int y = 0; y < sampleSize; y++)
                {
                    var position = new Vec2(
                        bounds.min.x + x * stepX,
                        bounds.min.y + y * stepY
                    );
                    heightMap[x, y] = scene.GetTerrainHeight(position);
                }
            }

            return heightMap;
        }

        private static bool HasRangedWeapon(Agent agent)
        {
            return agent.WieldedWeapon.CurrentUsageItem?.WeaponClass.ToString().Contains("Ranged") ?? false;
        }

        private static string GetUnitClass(Agent agent)
        {
            if (agent.IsMount) return "Mount";
            if (agent.HasMount) return "Cavalry";
            if (HasRangedWeapon(agent)) return "Ranged";
            return "Infantry";
        }
    }

    public class UnitData
    {
        public int Id { get; set; }
        public Vec3 Position { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public bool IsRanged { get; set; }
        public bool IsMounted { get; set; }
        public int FormationIndex { get; set; }
        public string Class { get; set; }
    }

    public class FormationData
    {
        public int FormationIndex { get; set; }
        public int UnitCount { get; set; }
        public Vec3 Position { get; set; }
        public Vec2 Direction { get; set; }
        public string FormationType { get; set; }
        public float Width { get; set; }
        public float Depth { get; set; }
        public bool IsRanged { get; set; }
        public bool IsMounted { get; set; }
    }

    public class TerrainData
    {
        public float[,] HeightMap { get; set; }
        public bool HasForest { get; set; }
        public bool HasHills { get; set; }
        public bool HasWater { get; set; }
    }

    public class WeatherData
    {
        public float TimeOfDay { get; set; }
        public bool IsNight { get; set; }
        public bool IsRaining { get; set; }
        public bool IsFoggy { get; set; }
    }
} 