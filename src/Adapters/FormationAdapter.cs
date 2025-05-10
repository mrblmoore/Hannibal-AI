using System;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;

namespace HannibalAI.Adapters
{
    /// <summary>
    /// Adapter to handle differences in the Bannerlord API for formations
    /// between different game versions
    /// </summary>
    public static class FormationAdapter
    {
        /// <summary>
        /// Gets the median position of a formation, handling API differences
        /// </summary>
        /// <param name="formation">The formation to get the position from</param>
        /// <returns>The median position as Vec3</returns>
        public static Vec3 GetMedianPosition(Formation formation)
        {
            if (formation == null)
                return Vec3.Zero;
                
            try
            {
                // In newer API versions, the position is accessed through formation.MedianPosition
                // In older versions, it might have been formation.Current.MedianPosition
                // We'll try directly first, then fall back to a default position
                
                // This approach uses reflection to handle API differences
                var mediaPositionProperty = formation.GetType().GetProperty("MedianPosition");
                if (mediaPositionProperty != null)
                {
                    // Try to get the position using reflection
                    var positionObj = mediaPositionProperty.GetValue(formation);
                    
                    // Try to get AsVec3 property if it exists
                    var asVec3Property = positionObj?.GetType().GetProperty("AsVec3");
                    if (asVec3Property != null)
                    {
                        return (Vec3)asVec3Property.GetValue(positionObj);
                    }
                    
                    // Try X, Y, Z properties as fallback
                    var xProperty = positionObj?.GetType().GetProperty("X");
                    var yProperty = positionObj?.GetType().GetProperty("Y");
                    var zProperty = positionObj?.GetType().GetProperty("Z");
                    
                    if (xProperty != null && yProperty != null)
                    {
                        float x = (float)xProperty.GetValue(positionObj);
                        float y = (float)yProperty.GetValue(positionObj);
                        float z = zProperty != null ? (float)zProperty.GetValue(positionObj) : 0f;
                        return new Vec3(x, y, z);
                    }
                }
                
                // If we can't get the position directly, use the Order Position as fallback
                var orderPos = formation.OrderPosition;
                return new Vec3(orderPos.X, orderPos.Y, 0f);
            }
            catch (Exception ex)
            {
                // Log the error but provide a usable result to avoid crashes
                Logger.Instance.Warning($"Failed to get formation position: {ex.Message}");
                var orderPos = formation.OrderPosition;
                return new Vec3(orderPos.X, orderPos.Y, 0f);
            }
        }
        
        /// <summary>
        /// Gets an enemy team from the mission, handling API differences
        /// </summary>
        /// <returns>The enemy team or null if not found</returns>
        public static Team GetEnemyTeam()
        {
            if (Mission.Current == null)
                return null;
                
            try
            {
                // In different API versions, the enemy team might be accessed differently
                // Try to find the enemy team based on relationship to player team
                
                if (Mission.Current.PlayerTeam != null)
                {
                    foreach (Team team in Mission.Current.Teams)
                    {
                        if (team != Mission.Current.PlayerTeam && team.IsEnemyOf(Mission.Current.PlayerTeam))
                        {
                            return team;
                        }
                    }
                }
                
                // Fallback to the first non-player team
                foreach (Team team in Mission.Current.Teams)
                {
                    if (team != Mission.Current.PlayerTeam)
                    {
                        return team;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Failed to get enemy team: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Creates a WorldPosition object from a Vec3, handling API differences
        /// </summary>
        /// <param name="scene">The current scene</param>
        /// <param name="position">The Vec3 position</param>
        /// <returns>A WorldPosition object</returns>
        public static object CreateWorldPosition(Scene scene, Vec3 position)
        {
            try
            {
                // Get the WorldPosition type through reflection
                var worldPosType = Type.GetType("TaleWorlds.Engine.WorldPosition, TaleWorlds.Engine");
                if (worldPosType == null)
                {
                    worldPosType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName == "TaleWorlds.Engine.WorldPosition");
                }
                
                if (worldPosType == null)
                {
                    // Log if we can't find the type
                    Logger.Instance.Warning("WorldPosition type not found, using fallback");
                    return null;
                }
                
                // Try to create WorldPosition using constructor with Scene and Vec3
                var constructor = worldPosType.GetConstructor(new[] { 
                    typeof(Scene), 
                    typeof(Vec3) 
                });
                
                if (constructor != null)
                {
                    return constructor.Invoke(new object[] { scene, position });
                }
                
                // Log if we can't find the constructor
                Logger.Instance.Warning("WorldPosition constructor not found, using fallback");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error creating WorldPosition: {ex.Message}");
                return null;
            }
        }
    }
}