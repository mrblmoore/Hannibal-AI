using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using HannibalAI.Memory;
using HannibalAI.Terrain;

namespace HannibalAI.Tactics
{
    /// <summary>
    /// Advanced formation control logic that selects optimal formations
    /// based on terrain, enemy positions, and tactical situation
    /// </summary>
    public class FormationController
    {
        private static FormationController _instance;
        
        public static FormationController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FormationController();
                }
                return _instance;
            }
        }
        
        // Formation types mapped to their tactical attributes
        private Dictionary<string, FormationTypeAttributes> _formationAttributes;
        
        // Battle context
        private Dictionary<Formation, Vec3> _lastPositions;
        private Dictionary<Formation, string> _currentFormationTypes;
        private Dictionary<Formation, FormationStatus> _formationStatus;
        private DateTime _lastFormationCheck;
        
        // Tactical constants
        private const float MINIMUM_FORMATION_CHANGE_INTERVAL = 10.0f; // Seconds
        private const float RANGED_THREAT_THRESHOLD = 0.4f; // Proportion of enemy units that are archers
        private const float CAVALRY_THREAT_THRESHOLD = 0.4f; // Proportion of enemy units that are cavalry
        private const float NEARBY_ENEMY_THRESHOLD = 50.0f; // Units within this distance are "nearby"
        private const float MINIMUM_FORMATION_DISTANCE = 20.0f; // Minimum distance to maintain between formations
        
        public FormationController()
        {
            _formationAttributes = new Dictionary<string, FormationTypeAttributes>();
            _lastPositions = new Dictionary<Formation, Vec3>();
            _currentFormationTypes = new Dictionary<Formation, string>();
            _formationStatus = new Dictionary<Formation, FormationStatus>();
            _lastFormationCheck = DateTime.Now;
            
            // Initialize formation attributes
            InitializeFormationAttributes();
            
            Logger.Instance.Info("FormationController initialized");
        }
        
        /// <summary>
        /// Initialize tactical attributes for each formation type
        /// </summary>
        private void InitializeFormationAttributes()
        {
            // Infantry formations
            _formationAttributes["Line"] = new FormationTypeAttributes
            {
                FormationType = "Line",
                SuitableFor = new List<FormationClass> { FormationClass.Infantry, FormationClass.Ranged },
                DefensiveValue = 0.5f,
                OffensiveValue = 0.5f,
                MobilityValue = 0.6f,
                RangedDefenseValue = 0.3f,
                CavalryDefenseValue = 0.3f,
                TerrainPreference = "Open",
                Description = "Standard line formation, balanced for most situations"
            };
            
            _formationAttributes["ShieldWall"] = new FormationTypeAttributes
            {
                FormationType = "ShieldWall",
                SuitableFor = new List<FormationClass> { FormationClass.Infantry },
                DefensiveValue = 0.9f,
                OffensiveValue = 0.2f,
                MobilityValue = 0.3f,
                RangedDefenseValue = 0.8f,
                CavalryDefenseValue = 0.5f,
                TerrainPreference = "Open",
                Description = "Defensive wall of shields, excellent against ranged attacks"
            };
            
            _formationAttributes["Square"] = new FormationTypeAttributes
            {
                FormationType = "Square",
                SuitableFor = new List<FormationClass> { FormationClass.Infantry },
                DefensiveValue = 0.8f,
                OffensiveValue = 0.3f,
                MobilityValue = 0.2f,
                RangedDefenseValue = 0.5f,
                CavalryDefenseValue = 0.8f,
                TerrainPreference = "Open",
                Description = "Defensive square, excellent against cavalry charges"
            };
            
            _formationAttributes["Loose"] = new FormationTypeAttributes
            {
                FormationType = "Loose",
                SuitableFor = new List<FormationClass> { FormationClass.Infantry, FormationClass.Ranged, FormationClass.HorseArcher },
                DefensiveValue = 0.2f,
                OffensiveValue = 0.4f,
                MobilityValue = 0.8f,
                RangedDefenseValue = 0.6f,
                CavalryDefenseValue = 0.1f,
                TerrainPreference = "Forest",
                Description = "Loose spacing, good for skirmishing and ranged units"
            };
            
            _formationAttributes["Circle"] = new FormationTypeAttributes
            {
                FormationType = "Circle",
                SuitableFor = new List<FormationClass> { FormationClass.Infantry, FormationClass.Ranged },
                DefensiveValue = 0.7f,
                OffensiveValue = 0.1f,
                MobilityValue = 0.2f,
                RangedDefenseValue = 0.6f,
                CavalryDefenseValue = 0.7f,
                TerrainPreference = "Open",
                Description = "All-around defense, good when surrounded"
            };
            
            _formationAttributes["Schiltron"] = new FormationTypeAttributes
            {
                FormationType = "Square", // Game engine equivalent
                SuitableFor = new List<FormationClass> { FormationClass.Infantry },
                DefensiveValue = 0.8f,
                OffensiveValue = 0.2f,
                MobilityValue = 0.2f,
                RangedDefenseValue = 0.3f,
                CavalryDefenseValue = 0.9f,
                TerrainPreference = "Open",
                Description = "Spear formation specifically designed to counter cavalry"
            };
            
            // Cavalry formations
            _formationAttributes["Wedge"] = new FormationTypeAttributes
            {
                FormationType = "Wedge",
                SuitableFor = new List<FormationClass> { FormationClass.Cavalry },
                DefensiveValue = 0.3f,
                OffensiveValue = 0.9f,
                MobilityValue = 0.7f,
                RangedDefenseValue = 0.2f,
                CavalryDefenseValue = 0.5f,
                TerrainPreference = "Open",
                Description = "Aggressive cavalry formation for breaking through enemy lines"
            };
            
            _formationAttributes["Column"] = new FormationTypeAttributes
            {
                FormationType = "Column",
                SuitableFor = new List<FormationClass> { FormationClass.Cavalry, FormationClass.Infantry },
                DefensiveValue = 0.4f,
                OffensiveValue = 0.6f,
                MobilityValue = 0.8f,
                RangedDefenseValue = 0.2f,
                CavalryDefenseValue = 0.4f,
                TerrainPreference = "Open",
                Description = "Mobile column for quick repositioning"
            };
            
            _formationAttributes["Skirmish"] = new FormationTypeAttributes
            {
                FormationType = "Loose", // Game engine equivalent
                SuitableFor = new List<FormationClass> { FormationClass.HorseArcher },
                DefensiveValue = 0.3f,
                OffensiveValue = 0.5f,
                MobilityValue = 0.9f,
                RangedDefenseValue = 0.4f,
                CavalryDefenseValue = 0.4f,
                TerrainPreference = "Open",
                Description = "Loose skirmishing formation for horse archers"
            };
        }
        
        /// <summary>
        /// Determine the optimal formation type for a given formation based on 
        /// current battlefield conditions and enemy formations
        /// </summary>
        public string GetOptimalFormationType(Formation formation, Team enemyTeam, BattlefieldContext context)
        {
            try
            {
                if (formation == null)
                {
                    return "Line"; // Default
                }
                
                // If we've changed formation recently, don't change again too soon
                if (_formationStatus.ContainsKey(formation) && 
                    (DateTime.Now - _formationStatus[formation].LastFormationChange).TotalSeconds < MINIMUM_FORMATION_CHANGE_INTERVAL)
                {
                    // Just return current formation type
                    if (_currentFormationTypes.ContainsKey(formation))
                    {
                        return _currentFormationTypes[formation];
                    }
                }
                
                // Determine basic formation class
                FormationClass formationClass = formation.FormationIndex;
                
                // Set default optimal type
                string optimalType = "Line"; // Default for most formations
                
                // Specific logic based on formation class
                switch (formationClass)
                {
                    case FormationClass.Infantry:
                        optimalType = GetOptimalInfantryFormation(formation, enemyTeam, context);
                        break;
                        
                    case FormationClass.Ranged:
                        optimalType = GetOptimalRangedFormation(formation, enemyTeam, context);
                        break;
                        
                    case FormationClass.Cavalry:
                        optimalType = GetOptimalCavalryFormation(formation, enemyTeam, context);
                        break;
                        
                    case FormationClass.HorseArcher:
                        optimalType = GetOptimalHorseArcherFormation(formation, enemyTeam, context);
                        break;
                }
                
                // Update formation records
                UpdateFormationStatus(formation, optimalType);
                
                // Log the formation change if it's different
                if (!_currentFormationTypes.ContainsKey(formation) || _currentFormationTypes[formation] != optimalType)
                {
                    string prevType = _currentFormationTypes.ContainsKey(formation) ? _currentFormationTypes[formation] : "None";
                    Logger.Instance.Info($"Formation changing from {prevType} to {optimalType} for {formationClass}");
                }
                
                return optimalType;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error getting optimal formation type: {ex.Message}");
                return "Line"; // Default fallback
            }
        }
        
        /// <summary>
        /// Determine optimal infantry formation based on tactical situation
        /// </summary>
        private string GetOptimalInfantryFormation(Formation formation, Team enemyTeam, BattlefieldContext context)
        {
            // Check if under ranged fire
            bool underRangedFire = IsUnderRangedFire(formation, enemyTeam);
            
            // Check for nearby cavalry threats
            bool cavalryThreat = IsFacingCavalryThreat(formation, enemyTeam);
            
            // Check for nearby enemies in general
            bool enemiesNearby = AreEnemiesNearby(formation, enemyTeam, NEARBY_ENEMY_THRESHOLD);
            
            // Check if surrounded
            bool surrounded = IsSurrounded(formation, enemyTeam);
            
            // Low health status
            bool isWeak = IsFormationWeak(formation);
            
            // Get terrain for current position
            string terrain = context.CurrentTerrain;
            bool isOnHighGround = context.OnHighGround;
            
            // Defensive behavior when weak
            if (isWeak)
            {
                if (cavalryThreat)
                {
                    return "Square";
                }
                else if (underRangedFire)
                {
                    return "ShieldWall";
                }
                else if (surrounded)
                {
                    return "Circle";
                }
                else
                {
                    return "Line";
                }
            }
            
            // Normal behavioral choices
            if (cavalryThreat)
            {
                return "Square"; // Good against cavalry
            }
            else if (underRangedFire)
            {
                return "ShieldWall"; // Best defense against ranged
            }
            else if (surrounded)
            {
                return "Circle"; // 360-degree defense
            }
            else if (terrain == "Forest")
            {
                return "Loose"; // Better in forests
            }
            else if (isOnHighGround && !enemiesNearby)
            {
                // Maintain line on high ground if enemies aren't close
                return "Line";
            }
            else if (enemiesNearby)
            {
                // Main battle line when enemies are close
                return "Line";
            }
            
            // Default formation for moving
            return "Column";
        }
        
        /// <summary>
        /// Determine optimal ranged formation based on tactical situation
        /// </summary>
        private string GetOptimalRangedFormation(Formation formation, Team enemyTeam, BattlefieldContext context)
        {
            // Primary checks for ranged units
            bool cavalryThreat = IsFacingCavalryThreat(formation, enemyTeam);
            bool enemiesClose = AreEnemiesNearby(formation, enemyTeam, NEARBY_ENEMY_THRESHOLD / 2); // Closer threshold for ranged
            bool isWeak = IsFormationWeak(formation);
            
            string terrain = context.CurrentTerrain;
            bool isOnHighGround = context.OnHighGround;
            
            // Emergency protective formations
            if (cavalryThreat && enemiesClose)
            {
                return "Square"; // Defensive against incoming cavalry
            }
            else if (enemiesClose)
            {
                return "Circle"; // Surrounded defense
            }
            else if (isWeak)
            {
                // Weak ranged units should maximize distance
                return "Loose";
            }
            
            // Normal positioning
            if (isOnHighGround)
            {
                // Standard line for firing from elevation
                return "Line";
            }
            else if (terrain == "Forest")
            {
                // Spread out in forests
                return "Loose";
            }
            
            // Default ranged formation
            return "Loose";
        }
        
        /// <summary>
        /// Determine optimal cavalry formation based on tactical situation
        /// </summary>
        private string GetOptimalCavalryFormation(Formation formation, Team enemyTeam, BattlefieldContext context)
        {
            // Cavalry-specific checks
            bool enemySpearWall = IsEnemyUsingSpearWall(enemyTeam);
            bool chargingOpportunity = HasClearChargeRoute(formation, enemyTeam);
            bool isWeak = IsFormationWeak(formation);
            
            string terrain = context.CurrentTerrain;
            
            // Avoid charging into spear walls
            if (enemySpearWall && AreEnemiesNearby(formation, enemyTeam, NEARBY_ENEMY_THRESHOLD))
            {
                return "Column"; // Avoid charging, maintain mobility
            }
            
            // Weak cavalry should avoid direct engagement
            if (isWeak)
            {
                return "Column";
            }
            
            // Forests are bad for cavalry
            if (terrain == "Forest")
            {
                return "Column"; // Tight formation in forests
            }
            
            // Good opportunity to charge
            if (chargingOpportunity && context.AggressionLevel > 0.6f)
            {
                return "Wedge"; // Aggressive charge formation
            }
            
            // Default cavalry formation for mobility
            return "Column";
        }
        
        /// <summary>
        /// Determine optimal horse archer formation based on tactical situation
        /// </summary>
        private string GetOptimalHorseArcherFormation(Formation formation, Team enemyTeam, BattlefieldContext context)
        {
            // Horse archers almost always want to be loose and mobile
            bool enemiesVeryClose = AreEnemiesNearby(formation, enemyTeam, NEARBY_ENEMY_THRESHOLD / 3);
            bool isWeak = IsFormationWeak(formation);
            
            // Emergency evasion
            if (enemiesVeryClose || isWeak)
            {
                return "Column"; // Tighter formation for quick movement away
            }
            
            // Default horse archer formation is loose for maximum mobility and firing arcs
            return "Loose";
        }
        
        /// <summary>
        /// Update the status record for a formation
        /// </summary>
        private void UpdateFormationStatus(Formation formation, string formationType)
        {
            if (!_formationStatus.ContainsKey(formation))
            {
                _formationStatus[formation] = new FormationStatus();
            }
            
            if (!_currentFormationTypes.ContainsKey(formation) || _currentFormationTypes[formation] != formationType)
            {
                _formationStatus[formation].LastFormationChange = DateTime.Now;
                _formationStatus[formation].FormationChangeCount++;
            }
            
            // Update current formation type
            _currentFormationTypes[formation] = formationType;
            
            // Update last position
            if (formation.QuerySystem != null)
            {
                WorldPosition position = formation.QuerySystem.MedianPosition;
                if (position.GetNavMesh() != null)
                {
                    _lastPositions[formation] = position.GetGroundVec3();
                }
            }
        }
        
        /// <summary>
        /// Check if a formation is under significant ranged fire
        /// </summary>
        private bool IsUnderRangedFire(Formation formation, Team enemyTeam)
        {
            if (formation == null || enemyTeam == null)
            {
                return false;
            }
            
            try
            {
                int arrowsIncoming = 0; // Would track actual projectiles in real implementation
                int rangedEnemyCount = 0;
                float totalDistance = 0f;
                
                // Find ranged enemy formations
                foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (enemyFormation.CountOfUnits <= 0) continue;
                    
                    if (enemyFormation.FormationIndex == FormationClass.Ranged ||
                        enemyFormation.FormationIndex == FormationClass.HorseArcher)
                    {
                        rangedEnemyCount += enemyFormation.CountOfUnits;
                        
                        // Calculate distance between formations
                        if (formation.QuerySystem != null && enemyFormation.QuerySystem != null)
                        {
                            WorldPosition myPos = formation.QuerySystem.MedianPosition;
                            WorldPosition enemyPos = enemyFormation.QuerySystem.MedianPosition;
                            
                            if (myPos.GetNavMesh() != null && enemyPos.GetNavMesh() != null)
                            {
                                Vec3 myVec = myPos.GetGroundVec3();
                                Vec3 enemyVec = enemyPos.GetGroundVec3();
                                
                                float distance = (myVec - enemyVec).Length;
                                totalDistance += distance;
                                
                                // Closer ranged units are more dangerous
                                if (distance < 80.0f) // Typical archer range
                                {
                                    arrowsIncoming += enemyFormation.CountOfUnits;
                                }
                            }
                        }
                    }
                }
                
                // Calculate average distance if there are ranged enemies
                float avgDistance = rangedEnemyCount > 0 ? totalDistance / rangedEnemyCount : float.MaxValue;
                
                // Determine if under significant fire
                return arrowsIncoming > 0 || (rangedEnemyCount > 10 && avgDistance < 100.0f);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error checking ranged fire status: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if a formation is facing a significant cavalry threat
        /// </summary>
        private bool IsFacingCavalryThreat(Formation formation, Team enemyTeam)
        {
            if (formation == null || enemyTeam == null)
            {
                return false;
            }
            
            try
            {
                int cavCount = 0;
                int totalEnemies = 0;
                
                // Count enemy cavalry within threatening distance
                foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (enemyFormation.CountOfUnits <= 0) continue;
                    
                    totalEnemies += enemyFormation.CountOfUnits;
                    
                    if (enemyFormation.FormationIndex == FormationClass.Cavalry)
                    {
                        // Check if cavalry is close enough to be a threat
                        if (IsFormationNearby(formation, enemyFormation, NEARBY_ENEMY_THRESHOLD * 1.5f)) // Cavalry has longer threat range
                        {
                            cavCount += enemyFormation.CountOfUnits;
                        }
                    }
                }
                
                // Calculate cavalry proportion
                float cavProportion = totalEnemies > 0 ? (float)cavCount / totalEnemies : 0f;
                
                // Threat exists if there are significant cavalry units nearby
                return cavCount > 10 || cavProportion > CAVALRY_THREAT_THRESHOLD;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error checking cavalry threat: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if enemies are nearby a formation
        /// </summary>
        private bool AreEnemiesNearby(Formation formation, Team enemyTeam, float threshold)
        {
            if (formation == null || enemyTeam == null)
            {
                return false;
            }
            
            try
            {
                // Check all enemy formations for proximity
                foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (enemyFormation.CountOfUnits <= 0) continue;
                    
                    if (IsFormationNearby(formation, enemyFormation, threshold))
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error checking nearby enemies: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if one formation is near another
        /// </summary>
        private bool IsFormationNearby(Formation formation1, Formation formation2, float threshold)
        {
            try
            {
                if (formation1.QuerySystem != null && formation2.QuerySystem != null)
                {
                    WorldPosition pos1 = formation1.QuerySystem.MedianPosition;
                    WorldPosition pos2 = formation2.QuerySystem.MedianPosition;
                    
                    if (pos1.GetNavMesh() != null && pos2.GetNavMesh() != null)
                    {
                        Vec3 vec1 = pos1.GetGroundVec3();
                        Vec3 vec2 = pos2.GetGroundVec3();
                        
                        float distance = (vec1 - vec2).Length;
                        return distance < threshold;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Check if a formation is surrounded by enemies
        /// </summary>
        private bool IsSurrounded(Formation formation, Team enemyTeam)
        {
            if (formation == null || enemyTeam == null)
            {
                return false;
            }
            
            try
            {
                // We'll consider a formation surrounded if there are enemy formations
                // in at least 3 different directions (forward, back, left, right)
                
                // Get formation position and orientation
                if (formation.QuerySystem == null)
                {
                    return false;
                }
                
                WorldPosition myPos = formation.QuerySystem.MedianPosition;
                if (myPos.GetNavMesh() == null)
                {
                    return false;
                }
                
                Vec3 myVec = myPos.GetGroundVec3();
                Vec3 dirForward = new Vec3(formation.Direction.X, formation.Direction.Y, 0);
                Vec3 dirRight = Vec3.CrossProduct(dirForward, new Vec3(0, 0, 1));
                
                // Count enemy positions in different quadrants
                int numQuadrantsWithEnemies = 0;
                bool[] quadrantHasEnemy = new bool[4]; // Forward, Right, Back, Left
                
                foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (enemyFormation.CountOfUnits <= 0) continue;
                    
                    if (enemyFormation.QuerySystem != null)
                    {
                        WorldPosition enemyPos = enemyFormation.QuerySystem.MedianPosition;
                        if (enemyPos.GetNavMesh() != null)
                        {
                            Vec3 enemyVec = enemyPos.GetGroundVec3();
                            Vec3 relativePos = enemyVec - myVec;
                            
                            // Skip if too far away
                            if (relativePos.Length > NEARBY_ENEMY_THRESHOLD)
                            {
                                continue;
                            }
                            
                            // Determine quadrant
                            float dotForward = Vec3.DotProduct(dirForward, relativePos);
                            float dotRight = Vec3.DotProduct(dirRight, relativePos);
                            
                            int quadrant;
                            if (dotForward >= 0 && dotRight >= 0) quadrant = 0; // Forward-Right
                            else if (dotForward < 0 && dotRight >= 0) quadrant = 1; // Back-Right
                            else if (dotForward < 0 && dotRight < 0) quadrant = 2; // Back-Left
                            else quadrant = 3; // Forward-Left
                            
                            quadrantHasEnemy[quadrant] = true;
                        }
                    }
                }
                
                // Count quadrants with enemies
                foreach (bool hasEnemy in quadrantHasEnemy)
                {
                    if (hasEnemy) numQuadrantsWithEnemies++;
                }
                
                // Surrounded if enemies in 3+ quadrants
                return numQuadrantsWithEnemies >= 3;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error checking if surrounded: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if formation has low health/is weakened
        /// </summary>
        private bool IsFormationWeak(Formation formation)
        {
            if (formation == null || formation.CountOfUnits <= 0)
            {
                return true;
            }
            
            try
            {
                // Calculate average health
                float totalHealth = 0f;
                int unitCount = 0;
                
                // Get agents from the formation using Bannerlord API
                List<Agent> formationAgents = Mission.Current.Agents.Where(
                    a => a != null && a.IsActive() && a.Formation == formation).ToList();
                
                foreach (Agent agent in formationAgents)
                {
                    if (agent != null && agent.IsActive())
                    {
                        totalHealth += agent.Health / agent.HealthLimit;
                        unitCount++;
                    }
                }
                
                float avgHealth = unitCount > 0 ? totalHealth / unitCount : 0f;
                
                // Check if formation is severely depleted
                // Since we don't have initial count, estimate based on current strength
                float initialEstimate = formation.CountOfUnits * 1.5f; // Estimate initial was 50% more
                float strengthRatio = formation.CountOfUnits / Math.Max(1, initialEstimate);
                
                // Consider weak if health is low or numbers are severely depleted
                return avgHealth < 0.4f || strengthRatio < 0.3f;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error checking formation weakness: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if enemy is using formations effective against cavalry
        /// </summary>
        private bool IsEnemyUsingSpearWall(Team enemyTeam)
        {
            if (enemyTeam == null)
            {
                return false;
            }
            
            try
            {
                foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (enemyFormation.CountOfUnits <= 0) continue;
                    
                    // Check if this is an infantry formation in a defensive stance
                    if (enemyFormation.FormationIndex == FormationClass.Infantry)
                    {
                        // Look for square/shieldwall or tight formations ready for cavalry
                        if (enemyFormation.ArrangementOrder != null &&
                            (enemyFormation.ArrangementOrder.OrderType.ToString().Contains("Square") ||
                            enemyFormation.ArrangementOrder.OrderType.ToString().Contains("ShieldWall")))
                        {
                            // In a real implementation, would check for units with spear weapons
                            // For now, just assume a significant portion have spears
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error checking for enemy spear walls: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Count units with spear-type weapons
        /// </summary>
        private int CountUnitsWithSpears(Formation formation)
        {
            int count = 0;
            
            try
            {
                // In real implementation would check agent weapon types
                // For now, approximate based on formation type
                if (formation.FormationIndex == FormationClass.Infantry)
                {
                    // Assume ~30% of infantry have spears by default
                    count = (int)(formation.CountOfUnits * 0.3f);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error counting spear units: {ex.Message}");
            }
            
            return count;
        }
        
        /// <summary>
        /// Check if there's a clear charge route for cavalry
        /// </summary>
        private bool HasClearChargeRoute(Formation formation, Team enemyTeam)
        {
            if (formation == null || enemyTeam == null)
            {
                return false;
            }
            
            try
            {
                // Get formation position and direction
                if (formation.QuerySystem == null) return false;
                
                WorldPosition myPos = formation.QuerySystem.MedianPosition;
                if (myPos.GetNavMesh() == null) return false;
                
                Vec3 myVec = myPos.GetGroundVec3();
                Vec3 chargeDirection = new Vec3(formation.Direction.X, formation.Direction.Y, 0);
                
                // Check charge path for obstacles
                foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (enemyFormation.CountOfUnits <= 0) continue;
                    
                    if (enemyFormation.QuerySystem != null)
                    {
                        WorldPosition enemyPos = enemyFormation.QuerySystem.MedianPosition;
                        if (enemyPos.GetNavMesh() != null)
                        {
                            Vec3 enemyVec = enemyPos.GetGroundVec3();
                            Vec3 relativePos = enemyVec - myVec;
                            
                            // Check if enemy is in front and at good charge distance
                            float dotProduct = Vec3.DotProduct(chargeDirection, relativePos.NormalizedCopy());
                            float distance = relativePos.Length;
                            
                            if (dotProduct > 0.7f && distance < 100f && distance > 30f)
                            {
                                // Good charge target
                                return true;
                            }
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error checking for clear charge route: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Clear formation status for new battle
        /// </summary>
        public void Reset()
        {
            _lastPositions.Clear();
            _currentFormationTypes.Clear();
            _formationStatus.Clear();
            _lastFormationCheck = DateTime.Now;
            
            Logger.Instance.Info("FormationController reset for new battle");
        }
    }
    
    /// <summary>
    /// Class tracking tactical attributes of formation types
    /// </summary>
    public class FormationTypeAttributes
    {
        public string FormationType { get; set; }
        public List<FormationClass> SuitableFor { get; set; } = new List<FormationClass>();
        public float DefensiveValue { get; set; }
        public float OffensiveValue { get; set; }
        public float MobilityValue { get; set; }
        public float RangedDefenseValue { get; set; }
        public float CavalryDefenseValue { get; set; }
        public string TerrainPreference { get; set; }
        public string Description { get; set; }
    }
    
    /// <summary>
    /// Status tracking for a formation
    /// </summary>
    public class FormationStatus
    {
        public DateTime LastFormationChange { get; set; } = DateTime.MinValue;
        public int FormationChangeCount { get; set; } = 0;
        public int KillCount { get; set; } = 0;
        public int DeathCount { get; set; } = 0;
        public float FormationEffectiveness { get; set; } = 0.5f;
    }
    
    /// <summary>
    /// Context information for battlefield decisions
    /// </summary>
    public class BattlefieldContext
    {
        public float AggressionLevel { get; set; } = 0.5f;
        public string CurrentTerrain { get; set; } = "Open";
        public bool OnHighGround { get; set; } = false;
        public bool HasCavalryAdvantage { get; set; } = false;
        public bool HasRangedAdvantage { get; set; } = false;
        public bool HasInfantryAdvantage { get; set; } = false;
        public bool UnderAttack { get; set; } = false;
        
        public BattlefieldContext()
        {
        }
        
        public BattlefieldContext(float aggressionLevel, string terrain)
        {
            AggressionLevel = aggressionLevel;
            CurrentTerrain = terrain;
        }
    }
}