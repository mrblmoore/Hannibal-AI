using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using HannibalAI.Adapters;

namespace HannibalAI.Terrain
{
    /// <summary>
    /// Analyzes battlefield terrain to identify tactical features and optimal positions
    /// </summary>
    public class TerrainAnalyzer
    {
        private static TerrainAnalyzer _instance;
        
        public static TerrainAnalyzer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TerrainAnalyzer();
                }
                return _instance;
            }
        }
        
        // Terrain analysis constants
        private const float HIGH_GROUND_THRESHOLD = 3.0f; // Meters
        private const float SIGNIFICANT_SLOPE_THRESHOLD = 15.0f; // Degrees
        private const float CHOKEPOINT_WIDTH_THRESHOLD = 15.0f; // Meters
        private const float FOREST_DENSITY_THRESHOLD = 0.3f; // Trees per unit area
        
        // Cache of terrain features
        private List<TerrainFeature> _terrainFeatures;
        private Dictionary<FormationClass, List<Vec3>> _optimalPositions;
        private TerrainType _currentTerrainType;
        private bool _terrainAnalyzed;
        private float _battlefieldWidth;
        private float _battlefieldLength;
        
        public TerrainAnalyzer()
        {
            _terrainFeatures = new List<TerrainFeature>();
            _optimalPositions = new Dictionary<FormationClass, List<Vec3>>();
            _terrainAnalyzed = false;
            
            // Initialize positions lists for each formation type
            foreach (FormationClass formationClass in Enum.GetValues(typeof(FormationClass)))
            {
                if (formationClass != FormationClass.NumberOfAllFormations)
                {
                    _optimalPositions[formationClass] = new List<Vec3>();
                }
            }
            
            Logger.Instance.Info("TerrainAnalyzer created");
        }
        
        /// <summary>
        /// Get the current terrain type
        /// </summary>
        public TerrainType GetTerrainType()
        {
            if (!_terrainAnalyzed)
            {
                AnalyzeCurrentTerrain();
            }
            return _currentTerrainType;
        }
        

        

        
        /// <summary>
        /// Analyze the current terrain to identify tactical features
        /// </summary>
        public List<TerrainFeature> AnalyzeCurrentTerrain()
        {
            if (_terrainAnalyzed)
            {
                return _terrainFeatures;
            }
            
            try
            {
                Logger.Instance.Info("Analyzing battlefield terrain...");
                _terrainFeatures.Clear();
                
                // Clear optimal positions
                foreach (var key in _optimalPositions.Keys)
                {
                    _optimalPositions[key].Clear();
                }
                
                // Get mission scene
                Scene scene = Mission.Current?.Scene;
                if (scene == null)
                {
                    Logger.Instance.Warning("Cannot analyze terrain - no active scene");
                    return _terrainFeatures;
                }
                
                // Determine battlefield bounds
                EstimateBattlefieldBounds();
                
                // Detect terrain type
                System.Diagnostics.Debug.Print("[HannibalAI] Detecting terrain type...");
                DetectTerrainType();
                
                // Scan for high ground
                System.Diagnostics.Debug.Print("[HannibalAI] Scanning for high ground...");
                ScanForHighGround();
                
                // Identify chokepoints
                System.Diagnostics.Debug.Print("[HannibalAI] Identifying chokepoints...");
                IdentifyChokepoints();
                
                // Find forest cover
                System.Diagnostics.Debug.Print("[HannibalAI] Finding forest cover...");
                FindForestCover();
                
                // Find water features
                System.Diagnostics.Debug.Print("[HannibalAI] Identifying water features...");
                FindWaterFeatures();
                
                // Identify flanking positions
                System.Diagnostics.Debug.Print("[HannibalAI] Finding flanking positions...");
                FindFlankingPositions();
                
                // Calculate optimal positions for each formation type
                System.Diagnostics.Debug.Print("[HannibalAI] Calculating optimal positions for formations...");
                CalculateOptimalPositions();
                
                // Log completion of terrain analysis
                System.Diagnostics.Debug.Print("[HannibalAI] Terrain analysis complete. Terrain type: " + _currentTerrainType);
                
                _terrainAnalyzed = true;
                
                Logger.Instance.Info($"Terrain analysis complete. Found {_terrainFeatures.Count} tactical features.");
                
                // Log feature counts by type if in debug mode
                if (ModConfig.Instance.Debug)
                {
                    Dictionary<TerrainFeatureType, int> featureCounts = new Dictionary<TerrainFeatureType, int>();
                    foreach (TerrainFeature feature in _terrainFeatures)
                    {
                        if (!featureCounts.ContainsKey(feature.FeatureType))
                        {
                            featureCounts[feature.FeatureType] = 0;
                        }
                        featureCounts[feature.FeatureType]++;
                    }
                    
                    // Log the counts
                    foreach (var kvp in featureCounts)
                    {
                        Logger.Instance.Info($"- {kvp.Key}: {kvp.Value} features");
                    }
                }
                
                return _terrainFeatures;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error analyzing terrain: {ex.Message}");
                return new List<TerrainFeature>();
            }
        }
        
        /// <summary>
        /// Get optimal positions for a specific formation type
        /// </summary>
        public List<Vec3> GetOptimalPositionsForFormation(FormationClass formationClass)
        {
            // Ensure terrain is analyzed
            if (!_terrainAnalyzed)
            {
                AnalyzeCurrentTerrain();
            }
            
            if (_optimalPositions.ContainsKey(formationClass))
            {
                return _optimalPositions[formationClass];
            }
            
            return new List<Vec3>();
        }
        
        /// <summary>
        /// Get terrain features of a specific type
        /// </summary>
        public List<TerrainFeature> GetTerrainFeaturesByType(TerrainFeatureType featureType)
        {
            // Ensure terrain is analyzed
            if (!_terrainAnalyzed)
            {
                AnalyzeCurrentTerrain();
            }
            
            return _terrainFeatures.FindAll(f => f.FeatureType == featureType);
        }
        
        /// <summary>
        /// Check if the player has a terrain advantage
        /// </summary>
        public bool HasTerrainAdvantage()
        {
            if (!_terrainAnalyzed)
            {
                AnalyzeCurrentTerrain();
            }
            
            // Check high ground control
            var highGroundFeatures = _terrainFeatures
                .Where(f => f.FeatureType == TerrainFeatureType.HighGround)
                .ToList();
                
            foreach (var feature in highGroundFeatures)
            {
                if (IsPositionControlledByPlayer(feature.Position))
                {
                    return true;
                }
            }
            
            // Check forest cover for archer advantage
            if (_currentTerrainType == TerrainType.Forest)
            {
                var archerFormations = Mission.Current?.PlayerTeam?.FormationsIncludingEmpty
                    .Where(f => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation)
                    .ToList();
                    
                if (archerFormations != null && archerFormations.Count > 0)
                {
                    foreach (var formation in archerFormations)
                    {
                        // If archers are in forest, they have advantage
                        var forestFeatures = _terrainFeatures
                            .Where(f => f.FeatureType == TerrainFeatureType.Forest)
                            .ToList();
                            
                        foreach (var forest in forestFeatures)
                        {
                            if ((FormationAdapter.GetMedianPosition(formation) - forest.Position).Length < forest.Size)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if a position is in forest
        /// </summary>
        private bool IsPositionInForest(Vec3 position)
        {
            return _terrainFeatures
                .Any(f => f.FeatureType == TerrainFeatureType.Forest && 
                     (f.Position - position).Length < f.Size);
        }
        
        /// <summary>
        /// Check if a position is controlled by the player
        /// </summary>
        private bool IsPositionControlledByPlayer(Vec3 position)
        {
            if (Mission.Current?.PlayerTeam == null)
            {
                return false;
            }
            
            float minDistToPlayer = float.MaxValue;
            float minDistToEnemy = float.MaxValue;
            
            // Check player formations
            foreach (var formation in Mission.Current.PlayerTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    float dist = (FormationAdapter.GetMedianPosition(formation) - position).Length;
                    minDistToPlayer = Math.Min(minDistToPlayer, dist);
                }
            }
            
            // Check enemy formations
            var enemyTeam = FormationAdapter.GetEnemyTeam();
            if (enemyTeam != null)
            {
                foreach (var formation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits > 0)
                    {
                        float dist = (FormationAdapter.GetMedianPosition(formation) - position).Length;
                        minDistToEnemy = Math.Min(minDistToEnemy, dist);
                    }
                }
            }
            
            // Position is controlled by player if player formations are closer
            return minDistToPlayer < minDistToEnemy;
        }
        
        /// <summary>
        /// Get the best high ground position for tactical advantage
        /// </summary>
        public Vec3 GetBestHighGroundPosition()
        {
            if (!_terrainAnalyzed)
            {
                AnalyzeCurrentTerrain();
            }
            
            // Get all high ground features
            var highGroundFeatures = _terrainFeatures
                .Where(f => f.FeatureType == TerrainFeatureType.HighGround)
                .OrderByDescending(f => f.Value)
                .ToList();
                
            if (highGroundFeatures.Count > 0)
            {
                // Return the highest one
                return highGroundFeatures[0].Position;
            }
            
            // If no high ground, return a reasonable central position
            return new Vec3(0, 0, 0);
        }
        
        /// <summary>
        /// Get the best defensive position (chokepoint or high ground)
        /// </summary>
        public Vec3 GetBestDefensivePosition()
        {
            if (!_terrainAnalyzed)
            {
                AnalyzeCurrentTerrain();
            }
            
            // Try to find a good chokepoint first
            var chokepoints = _terrainFeatures
                .Where(f => f.FeatureType == TerrainFeatureType.Chokepoint)
                .OrderBy(f => f.Size) // Smaller width is better for defense
                .ToList();
                
            if (chokepoints.Count > 0)
            {
                return chokepoints[0].Position;
            }
            
            // Otherwise use high ground
            return GetBestHighGroundPosition();
        }
        
        /// <summary>
        /// Get the best flanking position on a given side
        /// </summary>
        /// <param name="rightSide">True for right flank, false for left flank</param>
        public Vec3 GetBestFlankingPosition(bool rightSide)
        {
            if (!_terrainAnalyzed)
            {
                AnalyzeCurrentTerrain();
            }
            
            var flankingPositions = _terrainFeatures
                .Where(f => f.FeatureType == TerrainFeatureType.FlankingPosition)
                .ToList();
                
            if (flankingPositions.Count > 1)
            {
                // Get player and enemy positions
                Vec3 playerPos = GetPlayerStartPosition();
                Vec3 enemyPos = GetEnemyStartPositionPrimary();
                
                // Direction vectors
                Vec3 battleDirection = new Vec3(
                    enemyPos.x - playerPos.x,
                    enemyPos.y - playerPos.y,
                    0f
                );
                
                // Normalize
                float length = (float)Math.Sqrt(battleDirection.x * battleDirection.x + battleDirection.y * battleDirection.y);
                if (length > 0.01f)
                {
                    battleDirection.x /= length;
                    battleDirection.y /= length;
                }
                
                // Perpendicular vector (for identifying left/right)
                Vec3 perpendicular = new Vec3(-battleDirection.y, battleDirection.x, 0f);
                
                // For right side, we want the negative perpendicular direction
                if (rightSide)
                {
                    perpendicular.x = -perpendicular.x;
                    perpendicular.y = -perpendicular.y;
                }
                
                // Find positions on the requested side
                var positionsOnSide = new List<TerrainFeature>();
                
                foreach (var pos in flankingPositions)
                {
                    // Vector from player to position
                    Vec3 toPosition = new Vec3(
                        pos.Position.x - playerPos.x,
                        pos.Position.y - playerPos.y,
                        0f
                    );
                    
                    // Dot product with perpendicular vector tells us if it's on the correct side
                    float dot = toPosition.x * perpendicular.x + toPosition.y * perpendicular.y;
                    
                    if (dot > 0)
                    {
                        positionsOnSide.Add(pos);
                    }
                }
                
                if (positionsOnSide.Count > 0)
                {
                    // Return the one with highest tactical value
                    return positionsOnSide.OrderByDescending(p => p.Value).First().Position;
                }
            }
            
            // Default flanking position if no good terrain features found
            return GetDefaultFlankingPosition(rightSide);
        }
        
        /// <summary>
        /// Get a default flanking position when no terrain features are suitable
        /// </summary>
        private Vec3 GetDefaultFlankingPosition(bool rightSide)
        {
            Vec3 playerPos = GetPlayerStartPosition();
            Vec3 enemyPos = GetEnemyStartPositionPrimary();
            
            // Direction vectors
            Vec3 battleDirection = new Vec3(
                enemyPos.x - playerPos.x,
                enemyPos.y - playerPos.y,
                0f
            );
            
            // Normalize
            float length = (float)Math.Sqrt(battleDirection.x * battleDirection.x + battleDirection.y * battleDirection.y);
            if (length > 0.01f)
            {
                battleDirection.x /= length;
                battleDirection.y /= length;
            }
            
            // Perpendicular vector
            Vec3 perpendicular = new Vec3(-battleDirection.y, battleDirection.x, 0f);
            if (rightSide)
            {
                perpendicular.x = -perpendicular.x;
                perpendicular.y = -perpendicular.y;
            }
            
            // Create a point that's forward and to the side
            Vec3 flankPos = new Vec3(
                playerPos.x + battleDirection.x * (_battlefieldLength * 0.3f) + perpendicular.x * (_battlefieldWidth * 0.3f),
                playerPos.y + battleDirection.y * (_battlefieldLength * 0.3f) + perpendicular.y * (_battlefieldWidth * 0.3f),
                0f
            );
            
            return flankPos;
        }
        
        /// <summary>
        /// Calculate distance between two positions
        /// </summary>
        private float CalculateDistance(Vec3 pos1, Vec3 pos2)
        {
            float dx = pos1.x - pos2.x;
            float dy = pos1.y - pos2.y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
        
        /// <summary>
        /// Get player start position (center of player formations)
        /// </summary>
        private Vec3 GetPlayerStartPosition()
        {
            if (Mission.Current?.PlayerTeam == null)
            {
                // Default position
                return new Vec3(_battlefieldWidth * 0.25f, _battlefieldLength * 0.5f, 0f);
            }
            
            Vec3 center = Vec3.Zero;
            int count = 0;
            
            foreach (var formation in Mission.Current.PlayerTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    center += FormationAdapter.GetMedianPosition(formation);
                    count++;
                }
            }
            
            if (count > 0)
            {
                center /= count;
            }
            else
            {
                // Default position
                center = new Vec3(_battlefieldWidth * 0.25f, _battlefieldLength * 0.5f, 0f);
            }
            
            return center;
        }
        
        /// <summary>
        /// Get enemy start position (center of enemy formations)
        /// </summary>
        private Vec3 GetEnemyStartPositionPrimary()
        {
            var enemyTeam = FormationAdapter.GetEnemyTeam();
            if (enemyTeam == null)
            {
                // Default position
                return new Vec3(_battlefieldWidth * 0.75f, _battlefieldLength * 0.5f, 0f);
            }
            
            Vec3 center = Vec3.Zero;
            int count = 0;
            
            foreach (var formation in enemyTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    center += FormationAdapter.GetMedianPosition(formation);
                    count++;
                }
            }
            
            if (count > 0)
            {
                center /= count;
            }
            else
            {
                // Default position
                center = new Vec3(_battlefieldWidth * 0.75f, _battlefieldLength * 0.5f, 0f);
            }
            
            return center;
        }
        
        /// <summary>
        /// Reset terrain analysis when exiting battle
        /// </summary>
        public void Reset()
        {
            _terrainFeatures.Clear();
            foreach (var key in _optimalPositions.Keys)
            {
                _optimalPositions[key].Clear();
            }
            _terrainAnalyzed = false;
            
            Logger.Instance.Info("TerrainAnalyzer reset");
        }
        
        #region Terrain Analysis Methods
        
        /// <summary>
        /// Estimate the battlefield bounds based on spawn points or scene size
        /// </summary>
        private void EstimateBattlefieldBounds()
        {
            // In a real implementation, this would use spawn points and obstacles to determine
            // the actual usable battlefield area
            // For now, we'll use a simplified implementation with reasonable defaults
            
            _battlefieldWidth = 300.0f;
            _battlefieldLength = 300.0f;
            
            try
            {
                // Attempt to get better battlefield dimensions from mission bounds
                if (Mission.Current != null)
                {
                    // Example of how to get mission bounds - real implementation would use actual API methods
                    // Would need to get actual bounds from Mission.Current.Scene
                    _battlefieldWidth = 300.0f;
                    _battlefieldLength = 300.0f;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to estimate battlefield bounds: {ex.Message}");
            }
            
            Logger.Instance.Info($"Estimated battlefield dimensions: {_battlefieldWidth}m x {_battlefieldLength}m");
        }
        
        /// <summary>
        /// Detect the general terrain type of the battlefield
        /// </summary>
        private void DetectTerrainType()
        {
            // Default to plains
            _currentTerrainType = TerrainType.Plains;
            
            try
            {
                // In a real implementation, would analyze the distribution of terrain textures
                // and elevation variations to determine the terrain type
                // For now, use a simplified approach
                
                // In a real implementation, we would count terrain features by type
                // These would be incremented during scene analysis
                
                // Simulate terrain detection through scene analysis
                Scene scene = Mission.Current?.Scene;
                if (scene != null)
                {
                    // Placeholder for actual terrain detection code
                    // This would use texture identification, flora density, and elevation changes
                    
                    // For testing, use randomization to simulate different terrain types
                    Random random = new Random();
                    int terrainValue = random.Next(0, 5);
                    
                    switch (terrainValue)
                    {
                        case 0:
                            _currentTerrainType = TerrainType.Plains;
                            break;
                        case 1:
                            _currentTerrainType = TerrainType.Forest;
                            break;
                        case 2:
                            _currentTerrainType = TerrainType.Hills;
                            break;
                        case 3:
                            _currentTerrainType = TerrainType.Mountains;
                            break;
                        case 4:
                            _currentTerrainType = TerrainType.River;
                            break;
                    }
                    
                    // Override with more realistic detection in actual implementation
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to detect terrain type: {ex.Message}");
            }
            
            Logger.Instance.Info($"Detected terrain type: {_currentTerrainType}");
        }
        
        /// <summary>
        /// Scan the battlefield for elevated terrain that provides tactical advantage
        /// </summary>
        private void ScanForHighGround()
        {
            try
            {
                // In an actual implementation, would use heightmap sampling from the terrain
                // to identify significant high ground areas
                // For now, create some test high ground points
                
                // Create some sample high ground features
                Random random = new Random();
                int highGroundCount = random.Next(2, 5); // 2-4 high ground features
                
                for (int i = 0; i < highGroundCount; i++)
                {
                    // Create a high ground feature at a random position
                    float x = (random.Next(0, 100) / 100.0f) * _battlefieldWidth - (_battlefieldWidth / 2.0f);
                    float y = (random.Next(0, 100) / 100.0f) * _battlefieldLength - (_battlefieldLength / 2.0f);
                    
                    Vec3 position = new Vec3(x, y, 0.0f);
                    float radius = random.Next(10, 30); // 10-30m radius
                    float height = random.Next(3, 10); // 3-10m height
                    
                    TerrainFeature highGround = new TerrainFeature
                    {
                        FeatureType = TerrainFeatureType.HighGround,
                        Position = position,
                        Size = radius,
                        Value = height, // Value represents height advantage
                        Description = $"Hill ({height}m elevation)"
                    };
                    
                    _terrainFeatures.Add(highGround);
                    
                    // High ground is good for archers and infantry
                    _optimalPositions[FormationClass.Ranged].Add(position);
                    _optimalPositions[FormationClass.Infantry].Add(position);
                }
                
                Logger.Instance.Info($"Identified {highGroundCount} high ground features");
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to scan for high ground: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Identify narrower areas that can be used to funnel enemy forces
        /// </summary>
        private void IdentifyChokepoints()
        {
            try
            {
                // In a real implementation, would analyze terrain obstacles and pathfinding constraints
                // to identify natural chokepoints
                // For now, create some sample chokepoints
                
                Random random = new Random();
                int chokepointCount = random.Next(0, 3); // 0-2 chokepoints
                
                for (int i = 0; i < chokepointCount; i++)
                {
                    // Create a chokepoint at a random position
                    float x = (random.Next(0, 100) / 100.0f) * _battlefieldWidth - (_battlefieldWidth / 2.0f);
                    float y = (random.Next(0, 100) / 100.0f) * _battlefieldLength - (_battlefieldLength / 2.0f);
                    
                    Vec3 position = new Vec3(x, y, 0.0f);
                    float width = random.Next(5, 15); // 5-15m width
                    
                    TerrainFeature chokepoint = new TerrainFeature
                    {
                        FeatureType = TerrainFeatureType.Chokepoint,
                        Position = position,
                        Size = width,
                        Value = 1.0f, // Value represents tactical importance
                        Description = $"Chokepoint ({width}m wide)"
                    };
                    
                    _terrainFeatures.Add(chokepoint);
                    
                    // Chokepoints are good for infantry
                    _optimalPositions[FormationClass.Infantry].Add(position);
                }
                
                Logger.Instance.Info($"Identified {chokepointCount} chokepoints");
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to identify chokepoints: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Find areas with forest cover for ambush and tactical advantage
        /// </summary>
        private void FindForestCover()
        {
            try
            {
                // In a real implementation, would analyze flora density in different areas
                // to identify forests and significant tree cover
                // For now, create some sample forest areas
                
                Random random = new Random();
                int forestCount = _currentTerrainType == TerrainType.Forest ? 
                    random.Next(3, 6) : // More forests in forest terrain
                    random.Next(0, 3);  // Fewer forests in other terrain types
                
                for (int i = 0; i < forestCount; i++)
                {
                    // Create a forest area at a random position
                    float x = (random.Next(0, 100) / 100.0f) * _battlefieldWidth - (_battlefieldWidth / 2.0f);
                    float y = (random.Next(0, 100) / 100.0f) * _battlefieldLength - (_battlefieldLength / 2.0f);
                    
                    Vec3 position = new Vec3(x, y, 0.0f);
                    float radius = random.Next(15, 40); // 15-40m radius
                    float density = (random.Next(40, 100) / 100.0f); // 40-100% density
                    
                    TerrainFeature forest = new TerrainFeature
                    {
                        FeatureType = TerrainFeatureType.Forest,
                        Position = position,
                        Size = radius,
                        Value = density, // Value represents forest density
                        Description = $"Forest ({radius}m radius, {density*100}% density)"
                    };
                    
                    _terrainFeatures.Add(forest);
                    
                    // Forests are good for infantry and skirmishers, bad for cavalry
                    if (density > 0.7f)
                    {
                        _optimalPositions[FormationClass.Infantry].Add(position);
                    }
                }
                
                Logger.Instance.Info($"Identified {forestCount} forest areas");
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to find forest cover: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Find water features like rivers, lakes, or coastlines
        /// </summary>
        private void FindWaterFeatures()
        {
            try
            {
                // In a real implementation, would identify water bodies using texture and height analysis
                // For now, create some sample water features
                
                Random random = new Random();
                int waterCount = _currentTerrainType == TerrainType.River ? 
                    1 : // Always a river in river terrain
                    random.Next(0, 2); // 0-1 water features in other terrain
                
                for (int i = 0; i < waterCount; i++)
                {
                    // Decide if it's a river or lake
                    bool isRiver = (i == 0 && _currentTerrainType == TerrainType.River) || random.Next(0, 2) == 0;
                    
                    if (isRiver)
                    {
                        // Create a river crossing area at a random position
                        float x = (random.Next(0, 100) / 100.0f) * _battlefieldWidth - (_battlefieldWidth / 2.0f);
                        float y = (random.Next(0, 100) / 100.0f) * _battlefieldLength - (_battlefieldLength / 2.0f);
                        
                        Vec3 position = new Vec3(x, y, 0.0f);
                        float width = random.Next(5, 15); // 5-15m width
                        
                        TerrainFeature river = new TerrainFeature
                        {
                            FeatureType = TerrainFeatureType.River,
                            Position = position,
                            Size = width,
                            Value = 1.0f, // Value represents tactical importance
                            Description = $"River crossing ({width}m wide)"
                        };
                        
                        _terrainFeatures.Add(river);
                        
                        // River crossings are important to control
                        _optimalPositions[FormationClass.Infantry].Add(position);
                    }
                    else
                    {
                        // Create a lake at a random position
                        float x = (random.Next(0, 100) / 100.0f) * _battlefieldWidth - (_battlefieldWidth / 2.0f);
                        float y = (random.Next(0, 100) / 100.0f) * _battlefieldLength - (_battlefieldLength / 2.0f);
                        
                        Vec3 position = new Vec3(x, y, 0.0f);
                        float radius = random.Next(20, 50); // 20-50m radius
                        
                        TerrainFeature lake = new TerrainFeature
                        {
                            FeatureType = TerrainFeatureType.Lake,
                            Position = position,
                            Size = radius,
                            Value = 0.5f, // Value represents tactical importance
                            Description = $"Lake ({radius}m radius)"
                        };
                        
                        _terrainFeatures.Add(lake);
                    }
                }
                
                Logger.Instance.Info($"Identified {waterCount} water features");
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to find water features: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Identify open field areas suitable for cavalry and maneuvering
        /// </summary>
        private void FindOpenFields()
        {
            try
            {
                // In a real implementation, we would analyze terrain flatness, 
                // absence of obstacles, and sufficient space for cavalry maneuvers
                // For now, create sample open fields based on terrain type
                
                // Determine how many open fields to create based on terrain type
                Random random = new Random();
                int fieldCount = 0;
                
                switch (_currentTerrainType)
                {
                    case TerrainType.Plains:
                        fieldCount = random.Next(2, 5); // Plains have many open fields
                        break;
                    case TerrainType.Desert:
                        fieldCount = random.Next(3, 6); // Deserts have most open fields
                        break;
                    case TerrainType.Hills:
                        fieldCount = random.Next(1, 3); // Hills have some open fields
                        break;
                    case TerrainType.Forest:
                        fieldCount = random.Next(0, 2); // Forests have few open fields
                        break;
                    case TerrainType.Mountains:
                        fieldCount = random.Next(0, 1); // Mountains rarely have open fields
                        break;
                    default:
                        fieldCount = random.Next(1, 3); // Other terrain types have average number
                        break;
                }
                
                // Create open field features
                for (int i = 0; i < fieldCount; i++)
                {
                    // Create an open field at a random position
                    float x = (random.Next(0, 100) / 100.0f) * _battlefieldWidth - (_battlefieldWidth / 2.0f);
                    float y = (random.Next(0, 100) / 100.0f) * _battlefieldLength - (_battlefieldLength / 2.0f);
                    
                    Vec3 position = new Vec3(x, y, 0.0f);
                    float radius = random.Next(30, 80); // 30-80m radius of open area
                    
                    // Check if this position overlaps with water or forests (in a real implementation)
                    bool isValid = true;
                    foreach (var feature in _terrainFeatures)
                    {
                        if (feature.FeatureType == TerrainFeatureType.Forest || 
                            feature.FeatureType == TerrainFeatureType.River || 
                            feature.FeatureType == TerrainFeatureType.Lake)
                        {
                            float distance = CalculateDistance(feature.Position, position);
                            if (distance < feature.Size + radius * 0.5f)
                            {
                                // Overlaps with an incompatible feature
                                isValid = false;
                                break;
                            }
                        }
                    }
                    
                    if (isValid)
                    {
                        TerrainFeature openField = new TerrainFeature
                        {
                            FeatureType = TerrainFeatureType.OpenField,
                            Position = position,
                            Size = radius,
                            Value = 0.8f, // Value represents cavalry maneuverability
                            Description = $"Open field ({radius}m area)"
                        };
                        
                        _terrainFeatures.Add(openField);
                        
                        // Open fields are ideal for cavalry
                        _optimalPositions[FormationClass.Cavalry].Add(position);
                        _optimalPositions[FormationClass.HorseArcher].Add(position);
                    }
                }
                
                Logger.Instance.Info($"Identified {fieldCount} open field areas");
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to find open fields: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Identify positions that provide advantageous flanking opportunities
        /// </summary>
        private void FindFlankingPositions()
        {
            try
            {
                // In a real implementation, we would analyze the terrain and battle lines to identify
                // natural flanking positions based on terrain constraints and formation patterns
                // For now, we'll create strategic flanking positions based on the battlefield layout
                
                // First, determine the enemy's relative position and direction
                Vec3 playerStartPosition = GetPlayerStartPosition();
                Vec3 enemyStartPosition = GetEnemyStartPositionPrimary();
                
                // Direction vector from player to enemy
                Vec3 directionToEnemy = new Vec3(
                    enemyStartPosition.x - playerStartPosition.x,
                    enemyStartPosition.y - playerStartPosition.y,
                    0.0f
                );
                
                // Normalize
                float directionLength = (float)Math.Sqrt(
                    directionToEnemy.x * directionToEnemy.x + 
                    directionToEnemy.y * directionToEnemy.y);
                    
                if (directionLength > 0.01f)
                {
                    directionToEnemy.x /= directionLength;
                    directionToEnemy.y /= directionLength;
                }
                
                // Perpendicular direction for flanking (rotate 90 degrees)
                Vec3 flankDirection = new Vec3(
                    -directionToEnemy.y,
                    directionToEnemy.x,
                    0.0f
                );
                
                // Create flanking positions on both sides
                // Center point for flanking is between our position and enemy position, but closer to enemy
                Vec3 centerPosition = new Vec3(
                    playerStartPosition.x + directionToEnemy.x * (directionLength * 0.7f),
                    playerStartPosition.y + directionToEnemy.y * (directionLength * 0.7f),
                    0.0f
                );
                
                // Left flank position
                float flankDistance = _battlefieldWidth * 0.3f;
                Vec3 leftFlankPosition = new Vec3(
                    centerPosition.x + flankDirection.x * flankDistance,
                    centerPosition.y + flankDirection.y * flankDistance,
                    0.0f
                );
                
                TerrainFeature leftFlank = new TerrainFeature
                {
                    FeatureType = TerrainFeatureType.FlankingPosition,
                    Position = leftFlankPosition,
                    Size = 30.0f, // Area of influence
                    Value = 0.8f, // High tactical value
                    Description = "Left Flanking Position"
                };
                
                _terrainFeatures.Add(leftFlank);
                
                // Right flank position
                Vec3 rightFlankPosition = new Vec3(
                    centerPosition.x - flankDirection.x * flankDistance,
                    centerPosition.y - flankDirection.y * flankDistance,
                    0.0f
                );
                
                TerrainFeature rightFlank = new TerrainFeature
                {
                    FeatureType = TerrainFeatureType.FlankingPosition,
                    Position = rightFlankPosition,
                    Size = 30.0f, // Area of influence
                    Value = 0.8f, // High tactical value
                    Description = "Right Flanking Position"
                };
                
                _terrainFeatures.Add(rightFlank);
                
                // Add a rear flanking position if there's enough battlefield depth
                if (directionLength > _battlefieldLength * 0.4f)
                {
                    // Position behind enemy lines
                    Vec3 rearFlankPosition = new Vec3(
                        enemyStartPosition.x + directionToEnemy.x * (_battlefieldLength * 0.15f),
                        enemyStartPosition.y + directionToEnemy.y * (_battlefieldLength * 0.15f),
                        0.0f
                    );
                    
                    TerrainFeature rearFlank = new TerrainFeature
                    {
                        FeatureType = TerrainFeatureType.FlankingPosition,
                        Position = rearFlankPosition,
                        Size = 30.0f, // Area of influence
                        Value = 1.0f, // Highest tactical value
                        Description = "Rear Flanking Position"
                    };
                    
                    _terrainFeatures.Add(rearFlank);
                }
                
                // Flanking positions are good for cavalry and mobile troops
                foreach (var feature in _terrainFeatures.FindAll(f => f.FeatureType == TerrainFeatureType.FlankingPosition))
                {
                    _optimalPositions[FormationClass.Cavalry].Add(feature.Position);
                    _optimalPositions[FormationClass.HorseArcher].Add(feature.Position);
                }
                
                Logger.Instance.Info($"Identified {_terrainFeatures.FindAll(f => f.FeatureType == TerrainFeatureType.FlankingPosition).Count} flanking positions");
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to identify flanking positions: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Calculate optimal positions for each formation type based on terrain features
        /// </summary>
        private void CalculateOptimalPositions()
        {
            try
            {
                // Get battle info to understand the tactical situation
                BattleSideEnum playerSide = GetPlayerSide();
                Vec3 playerStartPosition = GetPlayerStartPosition();
                Vec3 enemyStartPosition = GetEnemyStartPositionPrimary();
                float battlefieldDepth = _battlefieldLength;
                
                // Direction vectors - from player to enemy and perpendicular
                Vec3 directionToEnemy = new Vec3(
                    enemyStartPosition.x - playerStartPosition.x,
                    enemyStartPosition.y - playerStartPosition.y,
                    0f);
                
                // Normalize direction vector
                float directionLength = (float)Math.Sqrt(
                    directionToEnemy.x * directionToEnemy.x + 
                    directionToEnemy.y * directionToEnemy.y);
                
                if (directionLength > 0.01f)
                {
                    directionToEnemy.x /= directionLength;
                    directionToEnemy.y /= directionLength;
                }
                
                // Perpendicular direction for flanking (rotate 90 degrees)
                Vec3 flankDirection = new Vec3(
                    -directionToEnemy.y,
                    directionToEnemy.x,
                    0f);
                
                Logger.Instance.Info($"Battle axis: {directionToEnemy.x:F2}, {directionToEnemy.y:F2}");
                
                // Define reference points for formation placement
                Vec3 frontLinePosition = new Vec3(
                    playerStartPosition.x + directionToEnemy.x * battlefieldDepth * 0.2f,
                    playerStartPosition.y + directionToEnemy.y * battlefieldDepth * 0.2f,
                    0f
                );
                
                Vec3 rearLinePosition = new Vec3(
                    playerStartPosition.x - directionToEnemy.x * battlefieldDepth * 0.1f,
                    playerStartPosition.y - directionToEnemy.y * battlefieldDepth * 0.1f,
                    0f
                );
                
                Vec3 leftFlankPosition = new Vec3(
                    frontLinePosition.x + flankDirection.x * _battlefieldWidth * 0.3f,
                    frontLinePosition.y + flankDirection.y * _battlefieldWidth * 0.3f,
                    0f
                );
                
                Vec3 rightFlankPosition = new Vec3(
                    frontLinePosition.x - flankDirection.x * _battlefieldWidth * 0.3f,
                    frontLinePosition.y - flankDirection.y * _battlefieldWidth * 0.3f,
                    0f
                );
                
                // Evaluate terrain features and create weighted positions for each formation class
                Dictionary<FormationClass, List<WeightedPosition>> weightedPositions = new Dictionary<FormationClass, List<WeightedPosition>>();
                
                // Initialize weighted positions for each formation type
                foreach (FormationClass formationClass in Enum.GetValues(typeof(FormationClass)))
                {
                    if (formationClass != FormationClass.NumberOfAllFormations)
                    {
                        weightedPositions[formationClass] = new List<WeightedPosition>();
                    }
                }
                
                // Add base positions with initial weights
                // Infantry frontline
                weightedPositions[FormationClass.Infantry].Add(new WeightedPosition(frontLinePosition, 0.5f, "Frontline"));
                
                // Archers rear position
                weightedPositions[FormationClass.Ranged].Add(new WeightedPosition(rearLinePosition, 0.5f, "Rear position"));
                
                // Cavalry flanking positions
                weightedPositions[FormationClass.Cavalry].Add(new WeightedPosition(leftFlankPosition, 0.5f, "Left flank"));
                weightedPositions[FormationClass.Cavalry].Add(new WeightedPosition(rightFlankPosition, 0.5f, "Right flank"));
                
                // Horse archers mobile positions
                weightedPositions[FormationClass.HorseArcher].Add(new WeightedPosition(
                    new Vec3(
                        rearLinePosition.x + flankDirection.x * _battlefieldWidth * 0.2f,
                        rearLinePosition.y + flankDirection.y * _battlefieldWidth * 0.2f,
                        0f
                    ), 
                    0.5f, 
                    "Mobile position"
                ));
                
                // Enhance weights based on terrain features
                foreach (var feature in _terrainFeatures)
                {
                    // Skip features that have no clear advantage
                    if (feature.Value <= 0.0f)
                        continue;
                    
                    // Calculate distance to frontline (for relevance)
                    float distToFront = CalculateDistance(feature.Position, frontLinePosition);
                    float relevance = 1.0f - Math.Min(distToFront / (_battlefieldLength * 0.5f), 1.0f);
                    
                    // Only consider features with reasonable relevance
                    if (relevance < 0.2f)
                        continue;
                        
                    // Determine which formation types benefit from this feature
                    switch (feature.FeatureType)
                    {
                        case TerrainFeatureType.HighGround:
                            // Weight is based on height advantage
                            float heightWeight = Math.Min(feature.Value / 10.0f, 1.0f) * relevance;
                            
                            // High ground is excellent for archers
                            weightedPositions[FormationClass.Ranged].Add(new WeightedPosition(
                                feature.Position, 
                                0.7f + heightWeight * 0.3f, 
                                $"High ground ({feature.Value:F1}m)"
                            ));
                            
                            // High ground is good for infantry in defensive positions
                            weightedPositions[FormationClass.Infantry].Add(new WeightedPosition(
                                feature.Position, 
                                0.5f + heightWeight * 0.5f, 
                                $"High ground ({feature.Value:F1}m)"
                            ));
                            break;
                            
                        case TerrainFeatureType.Chokepoint:
                            // Weight is based on how narrow the chokepoint is
                            float chokeWeight = Math.Min((CHOKEPOINT_WIDTH_THRESHOLD - feature.Size) / CHOKEPOINT_WIDTH_THRESHOLD, 0.8f);
                            chokeWeight = Math.Max(chokeWeight, 0.1f) * relevance;
                            
                            // Chokepoints are excellent for infantry
                            weightedPositions[FormationClass.Infantry].Add(new WeightedPosition(
                                feature.Position, 
                                0.6f + chokeWeight * 0.4f, 
                                $"Chokepoint ({feature.Size:F1}m)"
                            ));
                            
                            // Archers can support from behind chokepoints
                            Vec3 archeryPosition = new Vec3(
                                feature.Position.x - directionToEnemy.x * 15.0f,
                                feature.Position.y - directionToEnemy.y * 15.0f,
                                feature.Position.z
                            );
                            
                            weightedPositions[FormationClass.Ranged].Add(new WeightedPosition(
                                archeryPosition, 
                                0.4f + chokeWeight * 0.2f, 
                                $"Behind chokepoint"
                            ));
                            break;
                            
                        case TerrainFeatureType.Forest:
                            // Weight based on forest density
                            float forestWeight = Math.Min(feature.Value, 1.0f) * relevance;
                            
                            // Forests are good for infantry ambushes
                            weightedPositions[FormationClass.Infantry].Add(new WeightedPosition(
                                feature.Position, 
                                0.4f + forestWeight * 0.3f, 
                                $"Forest cover"
                            ));
                            
                            // Forests are terrible for cavalry
                            weightedPositions[FormationClass.Cavalry].Add(new WeightedPosition(
                                feature.Position, 
                                -0.5f, 
                                $"Forest (avoid)"
                            ));
                            break;
                            
                        case TerrainFeatureType.OpenField:
                            // Weight based on size of open area
                            float openWeight = Math.Min(feature.Size / 50.0f, 1.0f) * relevance;
                            
                            // Open fields are excellent for cavalry charges
                            weightedPositions[FormationClass.Cavalry].Add(new WeightedPosition(
                                feature.Position, 
                                0.6f + openWeight * 0.4f, 
                                $"Open field"
                            ));
                            
                            // Open fields are good for horse archers
                            weightedPositions[FormationClass.HorseArcher].Add(new WeightedPosition(
                                feature.Position, 
                                0.5f + openWeight * 0.3f, 
                                $"Open field"
                            ));
                            
                            // Infantry prefers not to be caught in the open
                            weightedPositions[FormationClass.Infantry].Add(new WeightedPosition(
                                feature.Position, 
                                0.2f, 
                                $"Open field (vulnerable)"
                            ));
                            break;
                            
                        case TerrainFeatureType.FlankingPosition:
                            // Weight based on tactical advantage
                            float flankWeight = Math.Min(feature.Value, 1.0f) * relevance;
                            
                            // Flanking positions are excellent for cavalry
                            weightedPositions[FormationClass.Cavalry].Add(new WeightedPosition(
                                feature.Position, 
                                0.7f + flankWeight * 0.3f, 
                                $"Flanking position"
                            ));
                            
                            // Horse archers can also use these positions
                            weightedPositions[FormationClass.HorseArcher].Add(new WeightedPosition(
                                feature.Position, 
                                0.6f + flankWeight * 0.2f, 
                                $"Flanking position"
                            ));
                            break;
                            
                        case TerrainFeatureType.River:
                        case TerrainFeatureType.Lake:
                            // Water features pose danger
                            weightedPositions[FormationClass.Infantry].Add(new WeightedPosition(
                                feature.Position, 
                                -0.8f, 
                                $"Water hazard (avoid)"
                            ));
                            
                            weightedPositions[FormationClass.Cavalry].Add(new WeightedPosition(
                                feature.Position, 
                                -0.9f, 
                                $"Water hazard (avoid)"
                            ));
                            break;
                            
                        case TerrainFeatureType.Bridge:
                            // Bridges are good defensive positions for infantry
                            weightedPositions[FormationClass.Infantry].Add(new WeightedPosition(
                                feature.Position, 
                                0.7f * relevance, 
                                $"Bridge position"
                            ));
                            break;
                    }
                }
                
                // Select best positions based on weights for each formation class
                foreach (var formationClass in weightedPositions.Keys)
                {
                    // Clear current optimal positions
                    _optimalPositions[formationClass].Clear();
                    
                    // Sort by weight and get top positions
                    var sortedPositions = weightedPositions[formationClass]
                        .OrderByDescending(wp => wp.Weight)
                        .ToList();
                    
                    // Get the top 3 positions (or fewer if not enough available)
                    int positionsToTake = Math.Min(3, sortedPositions.Count);
                    
                    for (int i = 0; i < positionsToTake; i++)
                    {
                        if (sortedPositions[i].Weight > 0.2f) // Only use positions with meaningful weight
                        {
                            _optimalPositions[formationClass].Add(sortedPositions[i].Position);
                            
                            if (ModConfig.Instance.VerboseLogging)
                            {
                                Logger.Instance.Info($"Optimal position for {formationClass}: {sortedPositions[i].Description} " +
                                    $"(Weight: {sortedPositions[i].Weight:F2})");
                            }
                        }
                    }
                    
                    // If no good positions found, use default positions
                    if (_optimalPositions[formationClass].Count == 0)
                    {
                        switch (formationClass)
                        {
                            case FormationClass.Infantry:
                                _optimalPositions[formationClass].Add(frontLinePosition);
                                break;
                            case FormationClass.Ranged:
                                _optimalPositions[formationClass].Add(rearLinePosition);
                                break;
                            case FormationClass.Cavalry:
                                _optimalPositions[formationClass].Add(rightFlankPosition);
                                break;
                            case FormationClass.HorseArcher:
                                _optimalPositions[formationClass].Add(leftFlankPosition);
                                break;
                        }
                    }
                }
                
                // Log the number of optimal positions found
                foreach (var kvp in _optimalPositions)
                {
                    Logger.Instance.Info($"Found {kvp.Value.Count} optimal positions for {kvp.Key}");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to calculate optimal positions: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get the player's side in the current battle
        /// </summary>
        private BattleSideEnum GetPlayerSide()
        {
            try
            {
                if (Mission.Current != null)
                {
                    // In a real implementation, would use Mission.Current.PlayerTeam.Side
                    // For now, assume attacker for consistency
                    return BattleSideEnum.Attacker;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to determine player side: {ex.Message}");
            }
            
            return BattleSideEnum.Attacker; // Default to attacker
        }
        
        /// <summary>
        /// Get the player's starting position in the battle (legacy method)
        /// </summary>
        private Vec3 GetPlayerStartPositionLegacy()
        {
            try
            {
                if (Mission.Current != null)
                {
                    // In a real implementation, would get spawn positions from Mission.Current
                    // For now, use a reasonable default based on battlefield dimensions
                    
                    if (GetPlayerSide() == BattleSideEnum.Attacker)
                    {
                        // Attackers typically start at one end of the battlefield
                        return new Vec3(0, -_battlefieldLength * 0.4f, 0);
                    }
                    else
                    {
                        // Defenders at the other end
                        return new Vec3(0, _battlefieldLength * 0.4f, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to get player start position: {ex.Message}");
            }
            
            // Default position if we can't determine it
            return new Vec3(0, -_battlefieldLength * 0.4f, 0);
        }
        
        /// <summary>
        /// Get the enemy's starting position in the battle (legacy method)
        /// </summary>
        private Vec3 GetEnemyStartPositionLegacy()
        {
            try
            {
                if (Mission.Current != null)
                {
                    // In a real implementation, would get spawn positions from Mission.Current
                    // For now, use a reasonable default based on battlefield dimensions and player position
                    
                    if (GetPlayerSide() == BattleSideEnum.Attacker)
                    {
                        // Defenders at the opposite end
                        return new Vec3(0, _battlefieldLength * 0.4f, 0);
                    }
                    else
                    {
                        // Attackers at the opposite end
                        return new Vec3(0, -_battlefieldLength * 0.4f, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to get enemy start position: {ex.Message}");
            }
            
            // Default position if we can't determine it
            return new Vec3(0, _battlefieldLength * 0.4f, 0);
        }
        

        /// <summary>
        /// Calculate the distance between two points (alt version)
        /// </summary>
        private float CalculateDistanceAlt(Vec3 a, Vec3 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            float dz = a.z - b.z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        
        #endregion
    }

    /// <summary>
    /// Types of terrain features that can be identified
    /// </summary>
    public enum TerrainFeatureType
    {
        HighGround,
        Chokepoint,
        Forest,
        River,
        Lake,
        Bridge,
        Cliff,
        OpenField,
        FlankingPosition
    }
    
    /// <summary>
    /// General terrain type of the battlefield
    /// </summary>
    public enum TerrainType
    {
        Plains,
        Forest,
        Hills,
        Mountains,
        River,
        Coast,
        Desert,
        Snow
    }
    
    /// <summary>
    /// Tactical feature of the terrain
    /// </summary>
    public class TerrainFeature
    {
        public TerrainFeatureType FeatureType { get; set; }
        public Vec3 Position { get; set; }
        public float Size { get; set; } // Radius or width depending on feature type
        public float Value { get; set; } // Height, density, or importance depending on feature type
        public string Description { get; set; }
    }
    
    /// <summary>
    /// Represents a position with a tactical weight for a specific formation
    /// </summary>
    public class WeightedPosition
    {
        public Vec3 Position { get; private set; }
        public float Weight { get; private set; }
        public string Description { get; private set; }
        
        /// <summary>
        /// Creates a new weighted position
        /// </summary>
        /// <param name="position">The 3D position</param>
        /// <param name="weight">Tactical weight (0-1 where higher is better)</param>
        /// <param name="description">Description of the position's tactical value</param>
        public WeightedPosition(Vec3 position, float weight, string description)
        {
            Position = position;
            Weight = weight;
            Description = description;
        }
    }
}