using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;

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
                    var worldPosition = (WorldPosition)mediaPositionProperty.GetValue(formation);
                    return worldPosition.AsVec3;
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
    }
}