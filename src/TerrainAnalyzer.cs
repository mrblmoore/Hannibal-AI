using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI
{
    /// <summary>
    /// Analyzes terrain features to provide tactical advantages
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
        
        // Terrain feature types
        public enum TerrainFeatureType
        {
            HighGround,
            Forest,
            OpenField,
            River,
            Bridge,
            Cliff,
            Valley,
            Hill
        }
        
        // Represents a significant terrain feature
        public class TerrainFeature
        {
            public TerrainFeatureType Type { get; set; }
            public Vec3 Position { get; set; }
            public float Radius { get; set; }
            public float TacticalValue { get; set; }
            public string Description { get; set; }
            
            public TerrainFeature(TerrainFeatureType type, Vec3 position, float radius, float tacticalValue)
            {
                Type = type;
                Position = position;
                Radius = radius;
                TacticalValue = tacticalValue;
                Description = GetDescription(type);
            }
            
            private string GetDescription(TerrainFeatureType type)
            {
                switch (type)
                {
                    case TerrainFeatureType.HighGround:
                        return "Elevated position providing tactical advantage";
                    case TerrainFeatureType.Forest:
                        return "Forest provides cover for infantry";
                    case TerrainFeatureType.OpenField:
                        return "Open area favorable for cavalry";
                    case TerrainFeatureType.River:
                        return "Water obstacle limiting movement";
                    case TerrainFeatureType.Bridge:
                        return "Chokepoint for defensive positions";
                    case TerrainFeatureType.Cliff:
                        return "Steep terrain preventing movement";
                    case TerrainFeatureType.Valley:
                        return "Depression that can conceal troops";
                    case TerrainFeatureType.Hill:
                        return "Elevated terrain providing visibility";
                    default:
                        return "Unknown terrain feature";
                }
            }
        }
        
        // Cache terrain analysis results
        private Dictionary<string, List<TerrainFeature>> _terrainCache;
        private string _currentMissionId;
        
        private TerrainAnalyzer()
        {
            _terrainCache = new Dictionary<string, List<TerrainFeature>>();
            _currentMissionId = "";
        }
        
        /// <summary>
        /// Analyze the current mission terrain for tactical features
        /// </summary>
        public List<TerrainFeature> AnalyzeCurrentTerrain()
        {
            try
            {
                // Skip if not in a mission
                if (Mission.Current == null)
                {
                    return new List<TerrainFeature>();
                }
                
                // Generate unique ID for current mission
                string missionId = GenerateMissionId();
                
                // Check if we have cached results
                if (_currentMissionId == missionId && _terrainCache.ContainsKey(missionId))
                {
                    return _terrainCache[missionId];
                }
                
                // Update current mission ID
                _currentMissionId = missionId;
                
                // Perform terrain analysis
                List<TerrainFeature> features = PerformTerrainAnalysis();
                
                // Cache the results
                _terrainCache[missionId] = features;
                
                // Debug output
                if (ModConfig.Instance.Debug)
                {
                    LogFeatures(features);
                }
                
                return features;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance.Debug)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Error in terrain analysis: {ex.Message}"));
                }
                return new List<TerrainFeature>();
            }
        }
        
        /// <summary>
        /// Find the nearest terrain feature of a specific type
        /// </summary>
        public TerrainFeature FindNearestFeature(TerrainFeatureType type, Vec3 position)
        {
            List<TerrainFeature> features = AnalyzeCurrentTerrain();
            TerrainFeature nearest = null;
            float minDistance = float.MaxValue;
            
            foreach (var feature in features)
            {
                if (feature.Type == type)
                {
                    float distance = position.Distance(feature.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = feature;
                    }
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Find tactical advantage points in the terrain
        /// </summary>
        public List<TerrainFeature> FindTacticalAdvantages(Vec3 referencePosition, float maxDistance = 100f)
        {
            List<TerrainFeature> features = AnalyzeCurrentTerrain();
            List<TerrainFeature> advantagePoints = new List<TerrainFeature>();
            
            foreach (var feature in features)
            {
                float distance = referencePosition.Distance(feature.Position);
                if (distance <= maxDistance && feature.TacticalValue > 0.5f)
                {
                    advantagePoints.Add(feature);
                }
            }
            
            // Sort by tactical value
            advantagePoints.Sort((a, b) => b.TacticalValue.CompareTo(a.TacticalValue));
            return advantagePoints;
        }
        
        /// <summary>
        /// Calculate the height advantage at a position
        /// </summary>
        public float GetHeightAdvantage(Vec3 position, Vec3 referencePosition)
        {
            return position.z - referencePosition.z;
        }
        
        /// <summary>
        /// Check if a position is on high ground relative to another
        /// </summary>
        public bool IsHighGround(Vec3 position, Vec3 referencePosition, float threshold = 3.0f)
        {
            return GetHeightAdvantage(position, referencePosition) >= threshold;
        }
        
        /// <summary>
        /// Find the best defensive position near a reference point
        /// </summary>
        public Vec3 FindBestDefensivePosition(Vec3 referencePosition, float searchRadius = 50f)
        {
            List<TerrainFeature> features = FindTacticalAdvantages(referencePosition, searchRadius);
            
            // Prioritize high ground and chokepoints
            foreach (var feature in features)
            {
                if (feature.Type == TerrainFeatureType.HighGround || 
                    feature.Type == TerrainFeatureType.Bridge)
                {
                    return feature.Position;
                }
            }
            
            // Fall back to most valuable feature
            if (features.Count > 0)
            {
                return features[0].Position;
            }
            
            // Default to reference position if no features found
            return referencePosition;
        }
        
        /// <summary>
        /// Check if position is in forest or dense vegetation
        /// </summary>
        public bool IsForestArea(Vec3 position)
        {
            // Implementation would check scene object density
            // For development, use placeholder logic
            List<TerrainFeature> features = AnalyzeCurrentTerrain();
            
            foreach (var feature in features)
            {
                if (feature.Type == TerrainFeatureType.Forest)
                {
                    float distance = position.Distance(feature.Position);
                    if (distance <= feature.Radius)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Calculate a world position's suitability for a formation type
        /// </summary>
        public float CalculatePositionSuitability(Vec3 position, FormationClass formationClass)
        {
            float suitability = 1.0f;
            
            // Adjust based on formation type and terrain
            switch (formationClass)
            {
                case FormationClass.Cavalry:
                    // Cavalry prefers open terrain
                    if (IsForestArea(position))
                    {
                        suitability *= 0.5f;
                    }
                    break;
                    
                case FormationClass.Infantry:
                    // Infantry benefits from forests for cover
                    if (IsForestArea(position))
                    {
                        suitability *= 1.2f;
                    }
                    
                    // Infantry benefits from high ground
                    List<TerrainFeature> terrainFeatures = AnalyzeCurrentTerrain();
                    if (terrainFeatures.Count > 0 && 
                        terrainFeatures[0].Type == TerrainFeatureType.HighGround && 
                        position.Distance(terrainFeatures[0].Position) < 20f)
                    {
                        suitability *= 1.3f;
                    }
                    break;
                    
                case FormationClass.Ranged:
                    // Archers benefit greatly from height
                    terrainFeatures = AnalyzeCurrentTerrain();
                    if (terrainFeatures.Count > 0 && 
                        terrainFeatures[0].Type == TerrainFeatureType.HighGround && 
                        position.Distance(terrainFeatures[0].Position) < 20f)
                    {
                        suitability *= 1.5f;
                    }
                    
                    // Archers don't want to be in forests
                    if (IsForestArea(position))
                    {
                        suitability *= 0.7f;
                    }
                    break;
                    
                case FormationClass.HorseArcher:
                    // Horse archers need open terrain 
                    if (IsForestArea(position))
                    {
                        suitability *= 0.4f;
                    }
                    break;
            }
            
            return Math.Max(0.1f, Math.Min(suitability, 2.0f));
        }
        
        /// <summary>
        /// Generate a unique ID for the current mission
        /// </summary>
        private string GenerateMissionId()
        {
            if (Mission.Current == null)
            {
                return "no_mission";
            }
            
            // Create a unique ID based on mission parameters
            return $"{Mission.Current.GetType().Name}_{Mission.Current.CombatType}_{DateTime.Now.ToShortDateString()}";
        }
        
        /// <summary>
        /// Perform terrain analysis on the current mission
        /// </summary>
        private List<TerrainFeature> PerformTerrainAnalysis()
        {
            var features = new List<TerrainFeature>();
            
            // For development we have to simulate some tactical features
            // In the real mod, this would use scene ray casting and height sampling
            
            // Generate sample terrain features 
            if (Mission.Current != null)
            {
                // In the actual game, we'd use Scene bounding box
                // For development, use placeholder values
                Vec3 missionCenter = new Vec3(100f, 100f, 0f);  
                float halfWidth = 100f;
                
                // Add a high ground feature
                features.Add(new TerrainFeature(
                    TerrainFeatureType.HighGround,
                    new Vec3(missionCenter.x + halfWidth * 0.3f, missionCenter.y + halfWidth * 0.2f, missionCenter.z + 10f),
                    15f,
                    0.9f
                ));
                
                // Add a forest feature
                features.Add(new TerrainFeature(
                    TerrainFeatureType.Forest,
                    new Vec3(missionCenter.x - halfWidth * 0.4f, missionCenter.y - halfWidth * 0.1f, missionCenter.z),
                    30f,
                    0.7f
                ));
                
                // Add a river/bridge feature
                features.Add(new TerrainFeature(
                    TerrainFeatureType.Bridge,
                    new Vec3(missionCenter.x, missionCenter.y - halfWidth * 0.3f, missionCenter.z),
                    10f,
                    0.8f
                ));
                
                // Add an open field feature
                features.Add(new TerrainFeature(
                    TerrainFeatureType.OpenField,
                    new Vec3(missionCenter.x - halfWidth * 0.2f, missionCenter.y + halfWidth * 0.4f, missionCenter.z),
                    40f,
                    0.6f
                ));
            }
            
            return features;
        }
        
        /// <summary>
        /// Log terrain features to debug output
        /// </summary>
        private void LogFeatures(List<TerrainFeature> features)
        {
            Logger.Instance.Info($"HannibalAI: Identified {features.Count} terrain features");
            
            foreach (var feature in features)
            {
                Logger.Instance.Info(
                    $"Terrain feature: {feature.Type} at ({feature.Position.x:F1}, {feature.Position.y:F1}, {feature.Position.z:F1}), value: {feature.TacticalValue:F2}"
                );
            }
        }
    }
}