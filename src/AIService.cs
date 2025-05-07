using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;

namespace HannibalAI
{
    /// <summary>
    /// Service class that creates and initializes AI components
    /// Provides tactical analysis and AI decision making
    /// </summary>
    public class AIService
    {
        private readonly ModConfig _config;
        private FallbackService _fallbackService;
        
        // Constants for tactical decision making
        private const float NEARBY_DISTANCE_THRESHOLD = 30f; // Units within this distance are considered "nearby"
        private const float WEAK_FORMATION_HEALTH_THRESHOLD = 0.4f; // Below this proportion of health, a formation is "weak"
        private const float HEIGHT_ADVANTAGE_THRESHOLD = 3.0f; // Height difference needed for terrain advantage
        
        public AIService(ModConfig config)
        {
            _config = config;
            _fallbackService = new FallbackService(config, this);
        }
        
        /// <summary>
        /// Process the current battle situation and generate AI decisions
        /// </summary>
        public List<FormationOrder> ProcessBattleSnapshot(Team playerTeam, Team enemyTeam)
        {
            List<FormationOrder> commands = new List<FormationOrder>();
            
            try
            {
                if (playerTeam == null || enemyTeam == null)
                {
                    return commands;
                }
                
                // Get and process formations
                foreach (Formation formation in playerTeam.FormationsIncludingEmpty)
                {
                    // Skip empty formations
                    if (formation.CountOfUnits <= 0)
                    {
                        continue;
                    }
                    
                    FormationOrder command = null;
                    
                    // Check tactical situation for this formation
                    bool isEnemyNearby = IsEnemyNearby(formation, enemyTeam);
                    bool isFormationWeak = IsFormationWeak(formation);
                    bool isTerrainAdvantageous = IsTerrainAdvantageous(formation.CurrentPosition.ToVec3());
                    
                    // Apply nemesis system logic if enabled
                    if (_config.UseCommanderMemory && CommanderMemoryService.Instance.HasVendettaAgainstPlayer)
                    {
                        // Commander with vendetta is more aggressive
                        float aggression = CommanderMemoryService.Instance.AggressivenessScore;
                        
                        if (aggression > 0.7f && isEnemyNearby)
                        {
                            // Very aggressive - always charge if enemy nearby
                            command = new FormationOrder
                            {
                                OrderType = FormationOrderType.Charge,
                                TargetFormation = formation,
                                TargetPosition = GetEnemyCenterPosition(enemyTeam),
                                AdditionalData = null
                            };
                        }
                        else if (aggression < 0.3f && isFormationWeak)
                        {
                            // Very cautious - always retreat if formation is weak
                            command = new FormationOrder
                            {
                                OrderType = FormationOrderType.Retreat,
                                TargetFormation = formation,
                                TargetPosition = Vec3.Zero,
                                AdditionalData = null
                            };
                        }
                    }
                    
                    // If no command from nemesis system, apply standard tactical logic
                    if (command == null)
                    {
                        // Primary tactical logic
                        if (isEnemyNearby && isFormationWeak)
                        {
                            // Weak formation with enemies nearby should hold position
                            command = new FormationOrder
                            {
                                OrderType = FormationOrderType.Move,
                                TargetFormation = formation,
                                TargetPosition = formation.CurrentPosition.ToVec3(),
                                AdditionalData = GetDefensiveFormationType(formation)
                            };
                        }
                        else if (isTerrainAdvantageous && formation.FormationIndex == FormationClass.Ranged)
                        {
                            // Ranged units on advantageous terrain should hold and fire
                            command = new FormationOrder
                            {
                                OrderType = FormationOrderType.FireAt,
                                TargetFormation = formation,
                                TargetPosition = GetEnemyCenterPosition(enemyTeam),
                                AdditionalData = "Loose" 
                            };
                        }
                        else if (formation.FormationIndex == FormationClass.Cavalry && !isFormationWeak)
                        {
                            // Healthy cavalry should flank or charge based on aggressiveness
                            if (_config.AggressiveCavalry)
                            {
                                command = new FormationOrder
                                {
                                    OrderType = FormationOrderType.Charge,
                                    TargetFormation = formation,
                                    TargetPosition = GetEnemyCenterPosition(enemyTeam),
                                    AdditionalData = null
                                };
                            }
                            else
                            {
                                command = new FormationOrder
                                {
                                    OrderType = FormationOrderType.Move,
                                    TargetFormation = formation,
                                    TargetPosition = GetFlankPosition(formation, enemyTeam),
                                    AdditionalData = "Wedge"
                                };
                            }
                        }
                        else if (formation.FormationIndex == FormationClass.Infantry)
                        {
                            // Infantry behavior based on settings
                            if (_config.DefensiveInfantry || isFormationWeak)
                            {
                                command = new FormationOrder
                                {
                                    OrderType = FormationOrderType.FormShieldWall,
                                    TargetFormation = formation,
                                    TargetPosition = formation.CurrentPosition.ToVec3(),
                                    AdditionalData = "ShieldWall"
                                };
                            }
                            else
                            {
                                command = new FormationOrder
                                {
                                    OrderType = FormationOrderType.Advance,
                                    TargetFormation = formation,
                                    TargetPosition = GetEnemyCenterPosition(enemyTeam),
                                    AdditionalData = "Line"
                                };
                            }
                        }
                    }
                    
                    // Add generated command if any
                    if (command != null)
                    {
                        commands.Add(command);
                        
                        // Record formation preference in commander memory if enabled
                        if (_config.UseCommanderMemory && command.AdditionalData != null)
                        {
                            CommanderMemoryService.Instance.RecordFormationPreference(command.AdditionalData);
                        }
                    }
                }
                
                // If no commands were generated, use fallback service
                if (commands.Count == 0)
                {
                    commands = _fallbackService.GenerateFallbackOrders(playerTeam, enemyTeam);
                }
                
                // Log if debug is enabled
                if (_config.Debug && commands.Count > 0)
                {
                    Logger.Instance.Debug($"Generated {commands.Count} formation orders");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error processing battle snapshot: {ex.Message}");
            }
            
            return commands;
        }
        
        /// <summary>
        /// Check if enemy units are near this formation
        /// </summary>
        public bool IsEnemyNearby(Formation formation, Team enemyTeam)
        {
            if (formation == null || enemyTeam == null)
            {
                return false;
            }
            
            Vec3 formationPosition = formation.CurrentPosition.ToVec3();
            
            foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
            {
                if (enemyFormation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                Vec3 enemyPosition = enemyFormation.CurrentPosition.ToVec3();
                float distance = formationPosition.Distance(enemyPosition);
                
                if (distance < NEARBY_DISTANCE_THRESHOLD)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if a formation has low health/morale
        /// </summary>
        public bool IsFormationWeak(Formation formation)
        {
            if (formation == null || formation.CountOfUnits <= 0)
            {
                return true;
            }
            
            // For game API compatibility, use a simpler approach based on the formation's strength
            // rather than iterating through all agents, which may have different API in the game
            
            // For game API compatibility, use a constant value as formation strength
            // since we can't access Morale directly in this version
            float formationStrength = 0.8f; // Assume reasonable morale
            
            // Also consider unit count ratio (current vs. initial)
            // Note: We don't have access to initial count, so use a threshold
            int currentCount = formation.CountOfUnits;
            int estimatedInitialCount = 30; // Rough estimate
            
            if (currentCount < estimatedInitialCount / 2)
            {
                // If unit count is less than 50% of estimated initial, formation is weak
                return true;
            }
            
            return formationStrength < WEAK_FORMATION_HEALTH_THRESHOLD;
        }
        
        /// <summary>
        /// Check if position offers terrain advantage
        /// </summary>
        public bool IsTerrainAdvantageous(Vec3 position)
        {
            try
            {
                if (Mission.Current == null)
                {
                    return false;
                }
                
                // In the actual game, we would check terrain height
                // For compatibility, use a simpler approach based on position Z value
                float height = position.z;
                
                // Compare to average enemy position height
                Vec3 enemyCenter = GetEnemyCenterPosition(Mission.Current.PlayerEnemyTeam);
                float enemyHeight = enemyCenter.z;
                
                // Check if this position is significantly higher than enemy position
                return (height - enemyHeight) > HEIGHT_ADVANTAGE_THRESHOLD;
            }
            catch
            {
                // If terrain check fails, return false
                return false;
            }
        }
        
        /// <summary>
        /// Get a suitable defensive formation type based on unit composition
        /// </summary>
        private string GetDefensiveFormationType(Formation formation)
        {
            if (formation == null || formation.CountOfUnits <= 0)
            {
                return "Line";
            }
            
            FormationClass formationClass = formation.FormationIndex;
            
            switch (formationClass)
            {
                case FormationClass.Infantry:
                    return "ShieldWall";
                case FormationClass.Ranged:
                    return "Loose";
                case FormationClass.Cavalry:
                    return "Column";
                case FormationClass.HorseArcher:
                    return "Loose";
                default:
                    return "Line";
            }
        }
        
        /// <summary>
        /// Get enemy center position
        /// </summary>
        private Vec3 GetEnemyCenterPosition(Team enemyTeam)
        {
            if (enemyTeam == null)
            {
                return Vec3.Zero;
            }
            
            Vec3 sum = Vec3.Zero;
            int count = 0;
            
            foreach (Formation formation in enemyTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    sum += formation.CurrentPosition.ToVec3();
                    count++;
                }
            }
            
            return count > 0 ? sum / count : Vec3.Zero;
        }
        
        /// <summary>
        /// Get a good flanking position for a formation
        /// </summary>
        private Vec3 GetFlankPosition(Formation formation, Team enemyTeam)
        {
            if (formation == null || enemyTeam == null)
            {
                return Vec3.Zero;
            }
            
            Vec3 enemyCenter = GetEnemyCenterPosition(enemyTeam);
            Vec3 currentPos = formation.CurrentPosition.ToVec3();
            
            // Get a position perpendicular to the line between current pos and enemy
            Vec3 direction = enemyCenter - currentPos;
            direction.Normalize();
            
            // Calculate perpendicular direction (rotate 90 degrees)
            Vec3 perpendicular = new Vec3(-direction.y, direction.x, 0);
            perpendicular.Normalize();
            
            // Distance to flank
            float flankDistance = 50f;
            
            // Get flanking position
            return enemyCenter + (perpendicular * flankDistance);
        }
        
        /// <summary>
        /// Create a new AI commander instance
        /// </summary>
        public AICommander CreateCommander()
        {
            try
            {
                return new AICommander(_config);
            }
            catch (Exception ex)
            {
                LogError($"Error creating AI commander: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Log informational message
        /// </summary>
        public void LogInfo(string message)
        {
            Logger.Instance.Info(message);
        }
        
        /// <summary>
        /// Log error message
        /// </summary>
        public void LogError(string message)
        {
            Logger.Instance.Error(message);
        }
    }
}
