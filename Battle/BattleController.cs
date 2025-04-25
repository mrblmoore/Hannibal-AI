using System;
using System.Threading.Tasks;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;
using HannibalAI.Config;
using HannibalAI.Services;
using HannibalAI.Command;
using System.Collections.Generic;
using TaleWorlds.Engine;

namespace HannibalAI.Battle
{
    public class BattleController
    {
        private Mission _mission;
        private BattleSnapshot _currentSnapshot;
        private List<BattleSnapshot> _battleHistory;
        private readonly AIService _aiService;
        private readonly ModConfig _config;
        private AICommander _aiCommander;
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 1.0f;

        public BattleController(Mission mission)
        {
            if (mission == null)
            {
                throw new ArgumentNullException(nameof(mission));
            }

            _mission = mission;
            _config = ModConfig.Instance;
            _aiService = new AIService(_config.AIEndpoint, _config.APIKey);
            _battleHistory = new List<BattleSnapshot>();
            _aiCommander = new AICommander();
            _lastUpdateTime = 0f;
        }

        public void Update(float dt)
        {
            if (_mission == null || !_mission.IsFieldBattle || !_mission.MissionStarted)
            {
                return;
            }

            _lastUpdateTime += dt;
            if (_lastUpdateTime < UPDATE_INTERVAL)
            {
                return;
            }

            _lastUpdateTime = 0f;
            UpdateBattleState();
        }

        private void UpdateBattleState()
        {
            try
            {
                if (!_config.Enabled || _mission == null || _mission.CombatType != Mission.MissionCombatType.Combat)
                {
                    return;
                }

                _currentSnapshot = CreateSnapshot();
                if (_currentSnapshot == null)
                {
                    Debug.Print("Failed to create battle snapshot");
                    return;
                }

                _battleHistory.Add(_currentSnapshot);

                var decision = _aiCommander.MakeDecision(_currentSnapshot);
                if (decision == null || decision.Commands == null || decision.Commands.Count == 0)
                {
                    return;
                }

                ExecuteAIDecision(decision);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error updating battle state: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            _mission = null;
            _currentSnapshot = null;
            _battleHistory?.Clear();
        }

        private void ExecuteAIDecision(AIDecision decision)
        {
            if (decision?.Commands == null || decision.Commands.Count == 0)
            {
                return;
            }

            foreach (var command in decision.Commands)
            {
                if (command == null)
                {
                    continue;
                }

                try
                {
                    switch (command)
                    {
                        case MoveFormationCommand moveCmd:
                            ExecuteMoveFormation(moveCmd);
                            break;
                        case AttackFormationCommand attackCmd:
                            ExecuteAttackFormation(attackCmd);
                            break;
                        case ChangeFormationCommand changeCmd:
                            ExecuteChangeFormation(changeCmd);
                            break;
                        default:
                            Debug.Print($"Unknown command type: {command.GetType().Name}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"Error executing command: {ex.Message}");
                }
            }
        }

        private void ExecuteMoveFormation(MoveFormationCommand command)
        {
            if (command == null || _mission?.Scene == null)
            {
                return;
            }

            var formation = GetFormation(command.FormationId);
            if (formation == null)
            {
                Debug.Print($"Formation {command.FormationId} not found");
                return;
            }

            try
            {
                var worldPosition = new WorldPosition(_mission.Scene, command.TargetPosition);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(worldPosition));
            }
            catch (Exception ex)
            {
                Debug.Print($"Error executing move formation command: {ex.Message}");
            }
        }

        private void ExecuteAttackFormation(AttackFormationCommand command)
        {
            if (command == null)
            {
                return;
            }

            var formation = GetFormation(command.FormationId);
            var targetFormation = GetFormation(command.TargetFormationId);
            
            if (formation == null || targetFormation == null)
            {
                Debug.Print($"Formation {command.FormationId} or target formation {command.TargetFormationId} not found");
                return;
            }

            try
            {
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                formation.SetTargetFormation(targetFormation);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error executing attack formation command: {ex.Message}");
            }
        }

        private void ExecuteChangeFormation(ChangeFormationCommand command)
        {
            if (command == null)
            {
                return;
            }

            var formation = GetFormation(command.FormationId);
            if (formation == null)
            {
                Debug.Print($"Formation {command.FormationId} not found");
                return;
            }

            try
            {
                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderCustom(command.Width);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error executing change formation command: {ex.Message}");
            }
        }

        private Formation GetFormation(int formationId)
        {
            if (_mission == null || formationId < 0)
            {
                return null;
            }

            foreach (var team in _mission.Teams)
            {
                if (team == null)
                {
                    continue;
                }

                foreach (var formation in team.FormationsIncludingEmpty)
                {
                    if (formation != null && formation.Index == formationId)
                    {
                        return formation;
                    }
                }
            }

            return null;
        }

        private BattleSnapshot CreateSnapshot()
        {
            if (_mission == null || _mission.Scene == null)
            {
                return null;
            }

            try
            {
                var snapshot = new BattleSnapshot
                {
                    Time = _mission.CurrentTime,
                    Scene = _mission.Scene
                };

                Vec3 min = Vec3.Zero;
                Vec3 max = Vec3.Zero;
                _mission.Scene.GetBoundingBox(out min, out max);
                snapshot.MapSize = max - min;

                snapshot.PlayerUnits = GetUnitsData(_mission.PlayerTeam);
                snapshot.EnemyUnits = GetUnitsData(_mission.PlayerEnemyTeam);
                snapshot.Weather = GetWeatherData(_mission);
                snapshot.Terrain = GetTerrainData(_mission.Scene);

                return snapshot;
            }
            catch (Exception ex)
            {
                Debug.Print($"Error creating battle snapshot: {ex.Message}");
                return null;
            }
        }

        private List<UnitData> GetUnitsData(Team team)
        {
            if (team == null)
            {
                return new List<UnitData>();
            }

            try
            {
                return team.ActiveAgents
                    .Where(a => a != null && a.IsActive())
                    .Select(a => new UnitData
                    {
                        UnitId = a.Index,
                        Position = a.Position,
                        Direction = a.LookDirection,
                        Health = a.Health,
                        MaxHealth = a.HealthLimit,
                        FormationIndex = a.Formation?.Index ?? -1,
                        IsPlayerControlled = a.IsPlayerControlled,
                        IsRanged = HasRangedWeapon(a)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.Print($"Error getting units data: {ex.Message}");
                return new List<UnitData>();
            }
        }

        private bool HasRangedWeapon(Agent agent)
        {
            if (agent == null)
            {
                return false;
            }

            try
            {
                return agent.WieldedWeapon?.CurrentUsageItem?.WeaponClass.ToString().Contains("Ranged") ?? false;
            }
            catch
            {
                return false;
            }
        }

        private TerrainData GetTerrainData(Scene scene)
        {
            if (scene == null)
            {
                return null;
            }

            try
            {
                return new TerrainData
                {
                    HasForest = EstimateHasForest(scene),
                    HasHills = EstimateHasHills(scene),
                    HasWater = EstimateHasWater(scene),
                    AverageHeight = GetAverageHeight(scene)
                };
            }
            catch (Exception ex)
            {
                Debug.Print($"Error getting terrain data: {ex.Message}");
                return null;
            }
        }

        private float GetAverageHeight(Scene scene)
        {
            if (scene == null)
            {
                return 0f;
            }

            try
            {
                var samples = new List<float>();
                for (int x = 0; x < 10; x++)
                {
                    for (int y = 0; y < 10; y++)
                    {
                        var pos = new Vec2(x * 10f, y * 10f);
                        samples.Add(scene.GetTerrainHeight(pos));
                    }
                }
                return samples.Average();
            }
            catch (Exception ex)
            {
                Debug.Print($"Error getting average height: {ex.Message}");
                return 0f;
            }
        }

        private bool EstimateHasForest(Scene scene)
        {
            if (scene == null)
            {
                return false;
            }

            try
            {
                var entities = new List<GameEntity>();
                scene.GetEntities(ref entities);
                return entities.Count(e => e?.Name?.ToLower().Contains("tree") == true) > 100;
            }
            catch (Exception ex)
            {
                Debug.Print($"Error estimating forest: {ex.Message}");
                return false;
            }
        }

        private bool EstimateHasHills(Scene scene)
        {
            if (scene == null)
            {
                return false;
            }

            try
            {
                var samples = new List<float>();
                for (int x = 0; x < 10; x++)
                {
                    for (int y = 0; y < 10; y++)
                    {
                        var pos = new Vec2(x * 10f, y * 10f);
                        samples.Add(scene.GetTerrainHeight(pos));
                    }
                }
                return samples.Max() - samples.Min() > 5f;
            }
            catch (Exception ex)
            {
                Debug.Print($"Error estimating hills: {ex.Message}");
                return false;
            }
        }

        private bool EstimateHasWater(Scene scene)
        {
            if (scene == null)
            {
                return false;
            }

            try
            {
                for (int x = 0; x < 5; x++)
                {
                    for (int y = 0; y < 5; y++)
                    {
                        var pos = new Vec2(x * 20f, y * 20f);
                        if (scene.GetWaterLevel(pos) > 0)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.Print($"Error estimating water: {ex.Message}");
                return false;
            }
        }

        private WeatherData GetWeatherData(Mission mission)
        {
            if (mission?.Scene == null)
            {
                return null;
            }

            try
            {
                var scene = mission.Scene;
                var timeOfDay = mission.CurrentTime;
                var atmosphere = scene.GetAtmosphere();
                
                if (atmosphere == null)
                {
                    return null;
                }

                return new WeatherData
                {
                    TimeOfDay = timeOfDay,
                    IsNight = timeOfDay < 0.25f || timeOfDay > 0.75f,
                    RainDensity = atmosphere.RainDensity,
                    FogDensity = atmosphere.FogDensity
                };
            }
            catch (Exception ex)
            {
                Debug.Print($"Error getting weather data: {ex.Message}");
                return null;
            }
        }
    }
} 