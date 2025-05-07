using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;
// Use TaleWorlds.Library.Vec3 rather than TaleWorlds.Engine.Vec3
using Vec3 = TaleWorlds.Library.Vec3;

namespace HannibalAI
{
    /// <summary>
    /// Core AI decision-making component that analyzes battle conditions and issues formation orders
    /// </summary>
    public class AICommander
    {
        private readonly ModConfig _config;
        private readonly CommandExecutor _commandExecutor;
        
        // Current battle state tracking
        private Team _playerTeam;
        private List<Formation> _playerFormations;
        private Team _enemyTeam;
        private List<Formation> _enemyFormations;
        
        // Terrain analysis cache
        private Dictionary<string, Vec3> _keyPositions;
        
        public AICommander(ModConfig config)
        {
            _config = config;
            _commandExecutor = CommandExecutor.Instance;
            _keyPositions = new Dictionary<string, Vec3>();
            _playerFormations = new List<Formation>();
            _enemyFormations = new List<Formation>();
        }
        
        /// <summary>
        /// Initialize the commander with the current battle state
        /// </summary>
        public void Initialize(Team playerTeam, Team enemyTeam)
        {
            _playerTeam = playerTeam;
            _enemyTeam = enemyTeam;
            
            RefreshFormations();
            AnalyzeTerrain();
            
            if (_config.VerboseLogging)
            {
                LogBattleState();
            }
        }
        
        /// <summary>
        /// Update AI state and issue new orders as needed
        /// </summary>
        public void Update(float dt)
        {
            if (_playerTeam == null || _enemyTeam == null)
            {
                return;
            }
            
            RefreshFormations();
            
            // Skip processing if battle is effectively over
            if (IsBattleDecided())
            {
                return;
            }
            
            // Determine tactical situation
            TacticalSituation situation = AnalyzeTacticalSituation();
            
            // Generate and execute orders based on the situation
            List<FormationOrder> orders = GenerateOrders(situation);
            ExecuteOrders(orders);
        }
        
        /// <summary>
        /// Analyze the current tactical situation and classify it
        /// </summary>
        private TacticalSituation AnalyzeTacticalSituation()
        {
            // Determine overall situation type (offensive, defensive, flanking, etc.)
            bool isOutnumbered = _playerFormations.Count < _enemyFormations.Count;
            bool hasHighGround = EvaluateTerrainAdvantage();
            bool hasRangedAdvantage = EvaluateRangedAdvantage();
            
            if (isOutnumbered && !hasHighGround)
            {
                return TacticalSituation.Defensive;
            }
            else if (hasRangedAdvantage)
            {
                return TacticalSituation.RangedFocus;
            }
            else if (hasHighGround)
            {
                return TacticalSituation.HighGroundAdvantage;
            }
            else
            {
                return TacticalSituation.Offensive;
            }
        }
        
        /// <summary>
        /// Generate appropriate formation orders based on the tactical situation
        /// </summary>
        private List<FormationOrder> GenerateOrders(TacticalSituation situation)
        {
            List<FormationOrder> orders = new List<FormationOrder>();
            
            switch (situation)
            {
                case TacticalSituation.Defensive:
                    GenerateDefensiveOrders(orders);
                    break;
                case TacticalSituation.Offensive:
                    GenerateOffensiveOrders(orders);
                    break;
                case TacticalSituation.RangedFocus:
                    GenerateRangedFocusOrders(orders);
                    break;
                case TacticalSituation.HighGroundAdvantage:
                    GenerateHighGroundOrders(orders);
                    break;
                default:
                    GenerateDefaultOrders(orders);
                    break;
            }
            
            return orders;
        }
        
        /// <summary>
        /// Execute the generated orders via the command executor
        /// </summary>
        private void ExecuteOrders(List<FormationOrder> orders)
        {
            foreach (var order in orders)
            {
                _commandExecutor.ExecuteOrder(order);
                
                if (_config.VerboseLogging)
                {
                    // Log order execution
                    string message = $"Order: {order.OrderType} to formation {order.TargetFormation.Index}";
                    InformationManager.DisplayMessage(new InformationMessage(message));
                }
            }
        }
        
        /// <summary>
        /// Generate orders for a defensive posture
        /// </summary>
        private void GenerateDefensiveOrders(List<FormationOrder> orders)
        {
            // Position infantry as a defensive line
            var infantry = GetFormationsByClass(FormationClass.Infantry);
            if (infantry.Count > 0)
            {
                var defensivePosition = _keyPositions.ContainsKey("defensive") 
                    ? _keyPositions["defensive"] 
                    : GetTerrainHighPoint();
                
                foreach (var formation in infantry)
                {
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.Move,
                        TargetFormation = formation,
                        TargetPosition = defensivePosition,
                        AdditionalData = "ShieldWall"
                    });
                }
            }
            
            // Position archers behind infantry
            var archers = GetFormationsByClass(FormationClass.Ranged);
            if (archers.Count > 0 && infantry.Count > 0)
            {
                Vec3 archerPosition = GetPositionBehind(GetCenterPosition(infantry), 10f);
                
                foreach (var formation in archers)
                {
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.Move,
                        TargetFormation = formation,
                        TargetPosition = archerPosition,
                        AdditionalData = "Loose"
                    });
                }
            }
            
            // Keep cavalry in reserve for counter-attacks
            var cavalry = GetFormationsByClass(FormationClass.Cavalry);
            if (cavalry.Count > 0)
            {
                Vec3 cavalryPosition = GetFlankPosition(true);
                
                foreach (var formation in cavalry)
                {
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.Move,
                        TargetFormation = formation,
                        TargetPosition = cavalryPosition,
                        AdditionalData = "Defensive"
                    });
                }
            }
        }
        
        /// <summary>
        /// Generate orders for an offensive posture
        /// </summary>
        private void GenerateOffensiveOrders(List<FormationOrder> orders)
        {
            var enemyCenter = GetEnemyCenter();
            
            // Order infantry to advance on enemy
            var infantry = GetFormationsByClass(FormationClass.Infantry);
            foreach (var formation in infantry)
            {
                orders.Add(new FormationOrder
                {
                    OrderType = FormationOrderType.Advance,
                    TargetFormation = formation,
                    TargetPosition = enemyCenter,
                    AdditionalData = "Line"
                });
            }
            
            // Order archers to provide covering fire
            var archers = GetFormationsByClass(FormationClass.Ranged);
            if (archers.Count > 0)
            {
                Vec3 archerPosition = GetPositionBehind(enemyCenter, -20f);
                
                foreach (var formation in archers)
                {
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.Move,
                        TargetFormation = formation,
                        TargetPosition = archerPosition,
                        AdditionalData = "Fire"
                    });
                }
            }
            
            // Order cavalry to flank
            var cavalry = GetFormationsByClass(FormationClass.Cavalry);
            if (cavalry.Count > 0)
            {
                Vec3 flankPosition = GetFlankPosition(false);
                
                foreach (var formation in cavalry)
                {
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.Charge,
                        TargetFormation = formation,
                        TargetPosition = flankPosition,
                        AdditionalData = "Flank"
                    });
                }
            }
        }
        
        /// <summary>
        /// Generate orders focusing on ranged advantage
        /// </summary>
        private void GenerateRangedFocusOrders(List<FormationOrder> orders)
        {
            var enemyCenter = GetEnemyCenter();
            
            // Position infantry to protect archers
            var infantry = GetFormationsByClass(FormationClass.Infantry);
            if (infantry.Count > 0)
            {
                Vec3 infantryPosition = GetPositionBetween(GetPlayerCenter(), enemyCenter, 0.7f);
                
                foreach (var formation in infantry)
                {
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.Move,
                        TargetFormation = formation,
                        TargetPosition = infantryPosition,
                        AdditionalData = "ShieldWall"
                    });
                }
            }
            
            // Position archers for maximum effect
            var archers = GetFormationsByClass(FormationClass.Ranged);
            if (archers.Count > 0)
            {
                Vec3 archerPosition = GetHighGroundPosition();
                
                foreach (var formation in archers)
                {
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.Move,
                        TargetFormation = formation,
                        TargetPosition = archerPosition,
                        AdditionalData = "Loose"
                    });
                }
                
                // Add fire order
                foreach (var formation in archers)
                {
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.FireAt,
                        TargetFormation = formation,
                        TargetPosition = enemyCenter,
                        AdditionalData = "Volley"
                    });
                }
            }
            
            // Position cavalry to protect flanks
            var cavalry = GetFormationsByClass(FormationClass.Cavalry);
            if (cavalry.Count > 0)
            {
                for (int i = 0; i < cavalry.Count; i++)
                {
                    bool rightFlank = i % 2 == 0;
                    Vec3 position = GetFlankProtectionPosition(rightFlank);
                    
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.Move,
                        TargetFormation = cavalry[i],
                        TargetPosition = position,
                        AdditionalData = "Guard"
                    });
                }
            }
        }
        
        /// <summary>
        /// Generate orders exploiting high ground advantage
        /// </summary>
        private void GenerateHighGroundOrders(List<FormationOrder> orders)
        {
            Vec3 highGroundPosition = GetHighGroundPosition();
            
            // Position infantry on high ground
            var infantry = GetFormationsByClass(FormationClass.Infantry);
            foreach (var formation in infantry)
            {
                orders.Add(new FormationOrder
                {
                    OrderType = FormationOrderType.Move,
                    TargetFormation = formation,
                    TargetPosition = highGroundPosition,
                    AdditionalData = "Line"
                });
            }
            
            // Position archers behind infantry on high ground
            var archers = GetFormationsByClass(FormationClass.Ranged);
            if (archers.Count > 0 && infantry.Count > 0)
            {
                Vec3 archerPosition = GetPositionBehind(highGroundPosition, 5f);
                
                foreach (var formation in archers)
                {
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.Move,
                        TargetFormation = formation,
                        TargetPosition = archerPosition,
                        AdditionalData = "Fire"
                    });
                }
            }
            
            // Position cavalry for flanking when enemy approaches
            var cavalry = GetFormationsByClass(FormationClass.Cavalry);
            if (cavalry.Count > 0)
            {
                for (int i = 0; i < cavalry.Count; i++)
                {
                    bool rightFlank = i % 2 == 0;
                    Vec3 position = GetFlankPosition(rightFlank);
                    
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.Move,
                        TargetFormation = cavalry[i],
                        TargetPosition = position,
                        AdditionalData = "ReadyToCharge"
                    });
                }
            }
        }
        
        /// <summary>
        /// Generate default fallback orders when no specific situation is identified
        /// </summary>
        private void GenerateDefaultOrders(List<FormationOrder> orders)
        {
            var enemyCenter = GetEnemyCenter();
            var playerCenter = GetPlayerCenter();
            
            // Simple formation setup
            var infantry = GetFormationsByClass(FormationClass.Infantry);
            foreach (var formation in infantry)
            {
                orders.Add(new FormationOrder
                {
                    OrderType = FormationOrderType.Move,
                    TargetFormation = formation,
                    TargetPosition = GetPositionBetween(playerCenter, enemyCenter, 0.6f),
                    AdditionalData = "Line"
                });
            }
            
            var archers = GetFormationsByClass(FormationClass.Ranged);
            foreach (var formation in archers)
            {
                orders.Add(new FormationOrder
                {
                    OrderType = FormationOrderType.Move,
                    TargetFormation = formation,
                    TargetPosition = GetPositionBehind(playerCenter, 10f),
                    AdditionalData = "Loose"
                });
            }
            
            var cavalry = GetFormationsByClass(FormationClass.Cavalry);
            foreach (var formation in cavalry)
            {
                orders.Add(new FormationOrder
                {
                    OrderType = FormationOrderType.Move,
                    TargetFormation = formation,
                    TargetPosition = GetFlankPosition(true),
                    AdditionalData = "Column"
                });
            }
        }
        
        /// <summary>
        /// Refresh the cached formation lists
        /// </summary>
        private void RefreshFormations()
        {
            _playerFormations.Clear();
            _enemyFormations.Clear();
            
            if (_playerTeam != null)
            {
                foreach (Formation formation in _playerTeam.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits > 0)
                    {
                        _playerFormations.Add(formation);
                    }
                }
            }
            
            if (_enemyTeam != null)
            {
                foreach (Formation formation in _enemyTeam.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits > 0)
                    {
                        _enemyFormations.Add(formation);
                    }
                }
            }
        }
        
        /// <summary>
        /// Analyze terrain to identify key tactical positions
        /// </summary>
        private void AnalyzeTerrain()
        {
            _keyPositions.Clear();
            
            // For now, just use simple positional calculations
            // In a complete implementation, this would analyze the actual terrain height map
            _keyPositions["highGround"] = GetTerrainHighPoint();
            _keyPositions["defensive"] = GetDefensivePosition();
            _keyPositions["leftFlank"] = GetFlankPosition(false);
            _keyPositions["rightFlank"] = GetFlankPosition(true);
        }
        
        /// <summary>
        /// Get all formations of a specific class from player team
        /// </summary>
        private List<Formation> GetFormationsByClass(FormationClass formationClass)
        {
            List<Formation> result = new List<Formation>();
            
            foreach (var formation in _playerFormations)
            {
                if (formation.FormationIndex == formationClass)
                {
                    result.Add(formation);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Helper to get the center position of all enemy formations
        /// </summary>
        private Vec3 GetEnemyCenter()
        {
            if (_enemyFormations.Count == 0)
            {
                return new Vec3(0, 0, 0);
            }
            
            Vec3 sum = new Vec3(0, 0, 0);
            foreach (var formation in _enemyFormations)
            {
                // Convert Vec2 to Vec3 using Z=0
                Vec2 pos = formation.CurrentPosition;
                sum += new Vec3(pos.X, pos.Y, 0);
            }
            
            return sum / _enemyFormations.Count;
        }
        
        /// <summary>
        /// Helper to get the center position of all player formations
        /// </summary>
        private Vec3 GetPlayerCenter()
        {
            if (_playerFormations.Count == 0)
            {
                return new Vec3(0, 0, 0);
            }
            
            Vec3 sum = new Vec3(0, 0, 0);
            foreach (var formation in _playerFormations)
            {
                // Convert Vec2 to Vec3 using Z=0
                Vec2 pos = formation.CurrentPosition;
                sum += new Vec3(pos.X, pos.Y, 0);
            }
            
            return sum / _playerFormations.Count;
        }
        
        /// <summary>
        /// Helper to get the center position of a list of formations
        /// </summary>
        private Vec3 GetCenterPosition(List<Formation> formations)
        {
            if (formations.Count == 0)
            {
                return new Vec3(0, 0, 0);
            }
            
            Vec3 sum = new Vec3(0, 0, 0);
            foreach (var formation in formations)
            {
                // Convert Vec2 to Vec3 using Z=0
                Vec2 pos = formation.CurrentPosition;
                sum += new Vec3(pos.X, pos.Y, 0);
            }
            
            return sum / formations.Count;
        }
        
        /// <summary>
        /// Helper to get a position behind another position
        /// </summary>
        private Vec3 GetPositionBehind(Vec3 position, float distance)
        {
            Vec3 directionToEnemy = GetEnemyCenter() - position;
            directionToEnemy.Normalize();
            
            return position - directionToEnemy * distance;
        }
        
        /// <summary>
        /// Helper to get a flank position
        /// </summary>
        private Vec3 GetFlankPosition(bool rightSide)
        {
            Vec3 enemyCenter = GetEnemyCenter();
            Vec3 playerCenter = GetPlayerCenter();
            
            Vec3 forward = enemyCenter - playerCenter;
            forward.Normalize();
            
            Vec3 right = new Vec3(-forward.y, forward.x, 0); // Perpendicular to forward
            if (!rightSide)
            {
                right = -right; // Left side
            }
            
            return enemyCenter + right * 50f;
        }
        
        /// <summary>
        /// Helper to get a flank protection position
        /// </summary>
        private Vec3 GetFlankProtectionPosition(bool rightSide)
        {
            Vec3 playerCenter = GetPlayerCenter();
            Vec3 enemyCenter = GetEnemyCenter();
            
            Vec3 forward = enemyCenter - playerCenter;
            forward.Normalize();
            
            Vec3 right = new Vec3(-forward.y, forward.x, 0); // Perpendicular to forward
            if (!rightSide)
            {
                right = -right; // Left side
            }
            
            return playerCenter + right * 30f;
        }
        
        /// <summary>
        /// Helper to get a position between two points with a weight
        /// </summary>
        private Vec3 GetPositionBetween(Vec3 a, Vec3 b, float weight)
        {
            return a * (1 - weight) + b * weight;
        }
        
        /// <summary>
        /// Estimate a high ground position
        /// In a full implementation, this would use terrain height data
        /// </summary>
        private Vec3 GetTerrainHighPoint()
        {
            Vec3 playerCenter = GetPlayerCenter();
            Vec3 enemyCenter = GetEnemyCenter();
            
            // For simulation, just use a position between player and enemy
            return GetPositionBetween(playerCenter, enemyCenter, 0.4f);
        }
        
        /// <summary>
        /// Estimate a good defensive position
        /// In a full implementation, this would use terrain analysis
        /// </summary>
        private Vec3 GetDefensivePosition()
        {
            return GetHighGroundPosition();
        }
        
        /// <summary>
        /// Get best high ground position based on terrain analysis
        /// </summary>
        private Vec3 GetHighGroundPosition()
        {
            return _keyPositions.ContainsKey("highGround") 
                ? _keyPositions["highGround"] 
                : GetTerrainHighPoint();
        }
        
        /// <summary>
        /// Check if terrain advantage exists
        /// </summary>
        private bool EvaluateTerrainAdvantage()
        {
            // Simplified evaluation
            return true;
        }
        
        /// <summary>
        /// Check if player has ranged advantage
        /// </summary>
        private bool EvaluateRangedAdvantage()
        {
            int playerRanged = CountUnitsInFormationsOfClass(FormationClass.Ranged, _playerFormations);
            int enemyRanged = CountUnitsInFormationsOfClass(FormationClass.Ranged, _enemyFormations);
            
            return playerRanged > enemyRanged * 1.2f;
        }
        
        /// <summary>
        /// Count units in formations of a specific class
        /// </summary>
        private int CountUnitsInFormationsOfClass(TaleWorlds.MountAndBlade.FormationClass formationClass, List<Formation> formations)
        {
            int count = 0;
            
            foreach (var formation in formations)
            {
                // Use direct comparison since FormationIndex is already a FormationClass
                if (formation.FormationIndex == formationClass)
                {
                    count += formation.CountOfUnits;
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Check if battle is already effectively decided
        /// </summary>
        private bool IsBattleDecided()
        {
            int playerCount = CountTotalUnits(_playerFormations);
            int enemyCount = CountTotalUnits(_enemyFormations);
            
            return playerCount <= 5 || enemyCount <= 5 || 
                   playerCount >= enemyCount * 5 || 
                   enemyCount >= playerCount * 5;
        }
        
        /// <summary>
        /// Count total units in a list of formations
        /// </summary>
        private int CountTotalUnits(List<Formation> formations)
        {
            int count = 0;
            
            foreach (var formation in formations)
            {
                count += formation.CountOfUnits;
            }
            
            return count;
        }
        
        /// <summary>
        /// Log current battle state for debugging
        /// </summary>
        private void LogBattleState()
        {
            string message = $"Battle state: {_playerFormations.Count} player formations vs {_enemyFormations.Count} enemy formations";
            Logger.Instance.Info(message);
        }
        
        /// <summary>
        /// Tactical situation types for AI decision making
        /// </summary>
        private enum TacticalSituation
        {
            Offensive,
            Defensive,
            RangedFocus,
            HighGroundAdvantage,
            Default
        }
        
        // Using TaleWorlds.MountAndBlade.FormationClass directly instead of private enum
    }
}
