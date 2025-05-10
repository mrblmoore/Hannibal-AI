using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using HannibalAI.Command;
using HannibalAI.Adapters;

namespace HannibalAI
{
    /// <summary>
    /// Service that provides fallback positions for retreating formations
    /// </summary>
    public class FallbackService
    {
        private static FallbackService _instance;
        
        /// <summary>
        /// Singleton instance of the FallbackService
        /// </summary>
        public static FallbackService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FallbackService();
                }
                return _instance;
            }
        }
        
        // Store cached retreat points for each team
        private Dictionary<Team, List<Vec3>> _retreatPointsByTeam = new Dictionary<Team, List<Vec3>>();
        
        // Time of last retreat point refresh
        private DateTime _lastRetreatPointsRefresh = DateTime.MinValue;
        
        // Refresh interval for retreat points (in seconds)
        private const int RETREAT_POINT_REFRESH_INTERVAL = 10;
        
        /// <summary>
        /// Private constructor to enforce singleton
        /// </summary>
        private FallbackService()
        {
            Logger.Instance.Info("FallbackService initialized");
            System.Diagnostics.Debug.Print("[HannibalAI] FallbackService initialized");
        }
        
        /// <summary>
        /// Gets a fallback order for a formation
        /// </summary>
        /// <param name="formation">The formation to get a fallback order for</param>
        /// <returns>A FormationOrder for fallback, or null if no fallback is possible</returns>
        public FormationOrder GetFallbackOrder(Formation formation)
        {
            if (formation == null)
            {
                Logger.Instance.Warning("FallbackService.GetFallbackOrder: formation is null");
                return null;
            }
            
            try
            {
                // Refresh retreat points if needed
                RefreshRetreatPointsIfNeeded();
                
                // Get the formation's team
                var team = formation.Team;
                if (team == null)
                {
                    Logger.Instance.Warning($"FallbackService.GetFallbackOrder: formation {formation.Index} has no team");
                    return null;
                }
                
                // Find a safe retreat position for this team
                Vec3 retreatPos = GetRetreatPosition(team);
                
                // Create our custom retreat order
                var hannibalOrder = HannibalFormationOrder.CreateRetreatOrder(formation, retreatPos);
                
                // Convert to vanilla formation order
                return hannibalOrder.ToFormationOrder();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error getting fallback order: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] FallbackService error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Refreshes retreat points if needed
        /// </summary>
        private void RefreshRetreatPointsIfNeeded()
        {
            // Check if we need to refresh retreat points
            if ((DateTime.Now - _lastRetreatPointsRefresh).TotalSeconds >= RETREAT_POINT_REFRESH_INTERVAL)
            {
                RefreshRetreatPoints();
                _lastRetreatPointsRefresh = DateTime.Now;
            }
        }
        
        /// <summary>
        /// Refreshes retreat points for all teams
        /// </summary>
        private void RefreshRetreatPoints()
        {
            try
            {
                System.Diagnostics.Debug.Print("[HannibalAI] Refreshing retreat points");
                
                // Clear existing retreat points
                _retreatPointsByTeam.Clear();
                
                // Get all teams in the current mission
                if (Mission.Current != null)
                {
                    foreach (var team in Mission.Current.Teams)
                    {
                        // Find retreat points for this team
                        List<Vec3> retreatPoints = CalculateRetreatPointsForTeam(team);
                        
                        // Store retreat points for this team
                        _retreatPointsByTeam[team] = retreatPoints;
                        
                        System.Diagnostics.Debug.Print($"[HannibalAI] Found {retreatPoints.Count} retreat points for team {team.TeamIndex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error refreshing retreat points: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error refreshing retreat points: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Calculates retreat points for a team
        /// </summary>
        /// <param name="team">The team to calculate retreat points for</param>
        /// <returns>A list of retreat points</returns>
        private List<Vec3> CalculateRetreatPointsForTeam(Team team)
        {
            List<Vec3> retreatPoints = new List<Vec3>();
            
            try
            {
                // Find the team's spawn point as a fallback
                Vec3 teamSpawnPoint = GetTeamSpawnPoint(team);
                if (teamSpawnPoint != Vec3.Zero)
                {
                    retreatPoints.Add(teamSpawnPoint);
                }
                
                // Find defensive positions based on terrain
                List<Vec3> defensivePositions = FindDefensivePositions(team);
                retreatPoints.AddRange(defensivePositions);
                
                // If no retreat points were found, add a default fallback based on team positions
                if (retreatPoints.Count == 0)
                {
                    Vec3 defaultFallback = CalculateDefaultFallbackPosition(team);
                    retreatPoints.Add(defaultFallback);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error calculating retreat points for team {team.TeamIndex}: {ex.Message}");
                
                // Add a very basic fallback in case of error - retreat 100 units behind team center
                retreatPoints.Add(CalculateSimpleFallbackPosition(team));
            }
            
            return retreatPoints;
        }
        
        /// <summary>
        /// Gets the team's spawn point
        /// </summary>
        /// <param name="team">The team</param>
        /// <returns>The team's spawn point</returns>
        private Vec3 GetTeamSpawnPoint(Team team)
        {
            if (team == null || Mission.Current == null)
                return Vec3.Zero;
            
            try
            {
                // Try to find the team's spawn point through reflection
                var teamSpawnMethod = team.GetType().GetMethod("GetTeamSpawnPosition");
                if (teamSpawnMethod != null)
                {
                    var result = teamSpawnMethod.Invoke(team, null);
                    if (result is Vec3 pos)
                    {
                        return pos;
                    }
                }
                
                // Fallback: use the position of the team's agents at the start
                if (team.TeamAgents.Count > 0)
                {
                    float sumX = 0f, sumY = 0f, sumZ = 0f;
                    foreach (var agent in team.TeamAgents)
                    {
                        var agentPos = agent.Position;
                        sumX += agentPos.x;
                        sumY += agentPos.y;
                        sumZ += agentPos.z;
                    }
                    
                    if (team.TeamAgents.Count > 0)
                    {
                        return new Vec3(
                            sumX / team.TeamAgents.Count,
                            sumY / team.TeamAgents.Count,
                            sumZ / team.TeamAgents.Count
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Error getting spawn point for team {team.TeamIndex}: {ex.Message}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error getting spawn point: {ex.Message}");
            }
            
            return Vec3.Zero;
        }
        
        /// <summary>
        /// Finds defensive positions based on terrain
        /// </summary>
        /// <param name="team">The team</param>
        /// <returns>A list of defensive positions</returns>
        private List<Vec3> FindDefensivePositions(Team team)
        {
            List<Vec3> defensivePositions = new List<Vec3>();
            
            // This is a placeholder for more advanced terrain analysis
            // In a full implementation, this would analyze terrain features like hills, cover, etc.
            
            return defensivePositions;
        }
        
        /// <summary>
        /// Calculates a default fallback position
        /// </summary>
        /// <param name="team">The team</param>
        /// <returns>The default fallback position</returns>
        private Vec3 CalculateDefaultFallbackPosition(Team team)
        {
            if (team == null || Mission.Current == null)
                return Vec3.Zero;
            
            try
            {
                // Find enemy team
                Team enemyTeam = null;
                foreach (var otherTeam in Mission.Current.Teams)
                {
                    if (otherTeam != team && team.IsEnemyOf(otherTeam))
                    {
                        enemyTeam = otherTeam;
                        break;
                    }
                }
                
                if (enemyTeam != null)
                {
                    // Get positions of all team members
                    Vec3 teamCenter = CalculateTeamCenter(team);
                    
                    // Get positions of all enemy team members
                    Vec3 enemyCenter = CalculateTeamCenter(enemyTeam);
                    
                    if (teamCenter != Vec3.Zero && enemyCenter != Vec3.Zero)
                    {
                        // Calculate direction vector away from enemy
                        Vec3 direction = new Vec3(
                            teamCenter.x - enemyCenter.x,
                            teamCenter.y - enemyCenter.y,
                            teamCenter.z - enemyCenter.z
                        );
                        
                        // Normalize direction
                        float magnitude = (float)Math.Sqrt(direction.x * direction.x + direction.y * direction.y + direction.z * direction.z);
                        if (magnitude > 0.001f)
                        {
                            direction.x /= magnitude;
                            direction.y /= magnitude;
                            direction.z /= magnitude;
                            
                            // Move 100 units in that direction
                            return new Vec3(
                                teamCenter.x + direction.x * 100f,
                                teamCenter.y + direction.y * 100f,
                                teamCenter.z + direction.z * 100f
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Warning($"Error calculating fallback position: {ex.Message}");
                System.Diagnostics.Debug.Print($"[HannibalAI] Error in fallback calc: {ex.Message}");
            }
            
            // Couldn't calculate based on enemy position, use simple fallback
            return CalculateSimpleFallbackPosition(team);
        }
        
        /// <summary>
        /// Calculates a simple fallback position (last resort)
        /// </summary>
        /// <param name="team">The team</param>
        /// <returns>A simple fallback position</returns>
        private Vec3 CalculateSimpleFallbackPosition(Team team)
        {
            Vec3 teamCenter = CalculateTeamCenter(team);
            
            // Check if we could get the team center
            if (teamCenter == Vec3.Zero)
            {
                // No team center, use the center of the map
                if (Mission.Current != null && Mission.Current.Scene != null)
                {
                    return new Vec3(0, 0, 0);
                }
                else
                {
                    // No mission or scene, just return zero
                    return Vec3.Zero;
                }
            }
            
            // Move back 100 units in X direction as an absolute last resort
            return new Vec3(teamCenter.x - 100f, teamCenter.y, teamCenter.z);
        }
        
        /// <summary>
        /// Calculates the center position of a team
        /// </summary>
        /// <param name="team">The team</param>
        /// <returns>The center position</returns>
        private Vec3 CalculateTeamCenter(Team team)
        {
            if (team == null || team.TeamAgents.Count == 0)
                return Vec3.Zero;
            
            float sumX = 0f, sumY = 0f, sumZ = 0f;
            int count = 0;
            
            foreach (var agent in team.TeamAgents)
            {
                var pos = agent.Position;
                sumX += pos.x;
                sumY += pos.y;
                sumZ += pos.z;
                count++;
            }
            
            if (count > 0)
            {
                return new Vec3(sumX / count, sumY / count, sumZ / count);
            }
            
            return Vec3.Zero;
        }
        
        /// <summary>
        /// Gets a retreat position for a team
        /// </summary>
        /// <param name="team">The team</param>
        /// <returns>A retreat position</returns>
        private Vec3 GetRetreatPosition(Team team)
        {
            // Check if we have retreat points for this team
            if (_retreatPointsByTeam.TryGetValue(team, out List<Vec3> retreatPoints) && retreatPoints.Count > 0)
            {
                // For now, just return the first retreat point
                // In a more advanced implementation, this would select the best retreat point
                // based on the formation's current position, enemy positions, terrain, etc.
                return retreatPoints[0];
            }
            
            // No retreat points for this team, calculate a simple fallback
            return CalculateSimpleFallbackPosition(team);
        }
    }
}