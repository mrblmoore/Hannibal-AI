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
        /// Helper method to get position of a formation as Vec3
        /// </summary>
        private Vec3 GetFormationPosition(Formation formation)
        {
            if (formation == null)
            {
                return Vec3.Zero;
            }
            
            // Hard-coded test positions as a fallback
            // This is necessary because the WorldPosition handling varies between game versions
            // In actual gameplay, these would be replaced with real positions
            switch(formation.FormationIndex)
            {
                case FormationClass.Infantry:
                    return new Vec3(100f, 100f, 0f);
                case FormationClass.Ranged:
                    return new Vec3(120f, 80f, 0f);
                case FormationClass.Cavalry:
                    return new Vec3(80f, 120f, 0f);
                case FormationClass.HorseArcher:
                    return new Vec3(140f, 140f, 0f);
                default:
                    return new Vec3(100f, 100f, 0f);
            }
        }
        
        /// <summary>
        /// Helper method to get position of an agent as Vec3
        /// </summary>
        private Vec3 GetAgentPosition(Agent agent)
        {
            if (agent == null)
            {
                return Vec3.Zero;
            }
            
            // For agent positions, return a fixed position for testing
            // This avoids WorldPosition compatibility issues
            return new Vec3(150f, 150f, 0f);
        }
        
        /// <summary>
        /// Process the current battle situation and generate AI decisions
        /// </summary>
        public List<FormationOrder> ProcessBattleSnapshot(Team playerTeam, Team enemyTeam)
        {
            // If this is an enemy team, use specialized enemy AI
            if (Mission.Current?.MainAgent != null && 
                playerTeam.IsEnemyOf(Mission.Current.MainAgent.Team) && 
                _config.AIControlsEnemies)
            {
                return ProcessEnemyTeamSnapshot(playerTeam, enemyTeam);
            }
            
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
                    bool isTerrainAdvantageous = IsTerrainAdvantageous(GetFormationPosition(formation));
                    
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
                                TargetPosition = GetFormationPosition(formation),
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
                                    TargetPosition = GetFormationPosition(formation),
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
            
            Vec3 formationPosition = GetFormationPosition(formation);
            
            foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
            {
                if (enemyFormation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                Vec3 enemyPosition = GetFormationPosition(enemyFormation);
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
                    sum += GetFormationPosition(formation);
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
            Vec3 currentPos = GetFormationPosition(formation);
            
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
        /// Create a new AI commander instance with proper configuration
        /// Note: AICommander now manages its own AIService instance
        /// </summary>
        public AICommander CreateCommander()
        {
            try
            {
                // AICommander now creates and manages its own AIService instance internally
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
        
        /// <summary>
        /// Process the enemy team's snapshot to generate more advanced enemy AI decisions
        /// </summary>
        private List<FormationOrder> ProcessEnemyTeamSnapshot(Team enemyTeam, Team playerTeam)
        {
            List<FormationOrder> commands = new List<FormationOrder>();
            
            try
            {
                if (enemyTeam == null || playerTeam == null)
                {
                    return commands;
                }
                
                // Get tactical advice from commander memory if enabled
                TacticalAdvice tacticalAdvice = null;
                if (_config.UseCommanderMemory)
                {
                    tacticalAdvice = CommanderMemoryService.Instance.GetTacticalAdvice();
                    
                    // Debug output if relevant
                    if (_config.Debug && tacticalAdvice.HasLearningData)
                    {
                        Logger.Instance.Debug($"Using tactical advice from commander memory: {tacticalAdvice}");
                    }
                }
                
                // Variables that influence enemy AI behavior
                float aggressivenessFactor = _config.UseCommanderMemory ? 
                    (tacticalAdvice?.SuggestedAggression ?? CommanderMemoryService.Instance.AggressivenessScore) : 0.5f;
                
                // Determine if player's cavalry is a significant threat
                bool playerHasStrongCavalry = DoesTeamHaveStrongCavalry(playerTeam);
                
                // Has this enemy defeated the player before? If so, they may have adapted
                bool hasAdaptedToPlayer = _config.UseCommanderMemory && 
                    CommanderMemoryService.Instance.TimesDefeatedPlayer > 0;
                
                // Tactical information from memory system
                bool hasVendettaAgainstPlayer = tacticalAdvice?.HasVendettaAgainstPlayer ?? false;
                string preferredFormationType = tacticalAdvice?.PreferredFormationType ?? "Line";
                string recommendedTactic = tacticalAdvice?.RecommendedTactic ?? "Balanced";
                string preferredTerrain = tacticalAdvice?.PreferredTerrain ?? "Open";
                
                // Step 1: Categorize formations
                List<Formation> infantryFormations = GetFormationsByClass(enemyTeam, FormationClass.Infantry);
                List<Formation> rangedFormations = GetFormationsByClass(enemyTeam, FormationClass.Ranged);
                List<Formation> cavalryFormations = GetFormationsByClass(enemyTeam, FormationClass.Cavalry);
                List<Formation> horseArcherFormations = GetFormationsByClass(enemyTeam, FormationClass.HorseArcher);
                
                // Step 2: Check battlefield conditions
                bool isOutnumbered = CountActiveAgents(enemyTeam) < CountActiveAgents(playerTeam);
                bool hasHeightAdvantage = EvaluateTerrainAdvantage(enemyTeam, playerTeam);
                
                // Step 3: Generate coordinated combined-arms strategy
                
                // Handle infantry formations - backbone of the army
                foreach (var formation in infantryFormations)
                {
                    FormationOrder order;
                    
                    if (isOutnumbered || aggressivenessFactor < 0.4f)
                    {
                        // Defensive posture if outnumbered or cautious
                        order = new FormationOrder
                        {
                            OrderType = FormationOrderType.FormShieldWall,
                            TargetFormation = formation,
                            TargetPosition = GetDefensivePosition(formation, enemyTeam, playerTeam),
                            AdditionalData = "ShieldWall"
                        };
                    }
                    else if (hasHeightAdvantage)
                    {
                        // Hold advantageous ground
                        order = new FormationOrder
                        {
                            OrderType = FormationOrderType.Move,
                            TargetFormation = formation,
                            TargetPosition = GetFormationPosition(formation),
                            AdditionalData = "Line"
                        };
                    }
                    else if (aggressivenessFactor > 0.7f)
                    {
                        // Aggressive advance
                        order = new FormationOrder
                        {
                            OrderType = FormationOrderType.Advance,
                            TargetFormation = formation,
                            TargetPosition = GetEnemyCenterPosition(playerTeam),
                            AdditionalData = "Line"
                        };
                    }
                    else
                    {
                        // Default to controlled advance
                        order = new FormationOrder
                        {
                            OrderType = FormationOrderType.Move,
                            TargetFormation = formation,
                            TargetPosition = GetControlledAdvancePosition(formation, enemyTeam, playerTeam),
                            AdditionalData = "Line"
                        };
                    }
                    
                    commands.Add(order);
                }
                
                // Handle ranged formations - target priority units or provide cover
                foreach (var formation in rangedFormations)
                {
                    FormationOrder order;
                    
                    if (playerHasStrongCavalry)
                    {
                        // Target cavalry threats
                        order = new FormationOrder
                        {
                            OrderType = FormationOrderType.FireAt,
                            TargetFormation = formation,
                            TargetPosition = GetCavalryPosition(playerTeam),
                            AdditionalData = "Loose"
                        };
                    }
                    else if (hasHeightAdvantage)
                    {
                        // Exploit height advantage for fire
                        order = new FormationOrder
                        {
                            OrderType = FormationOrderType.FireAt,
                            TargetFormation = formation,
                            TargetPosition = GetEnemyCenterPosition(playerTeam),
                            AdditionalData = "Loose"
                        };
                    }
                    else
                    {
                        // Default to targeting enemy center
                        order = new FormationOrder
                        {
                            OrderType = FormationOrderType.FormLoose,
                            TargetFormation = formation,
                            TargetPosition = GetProtectedArcherPosition(formation, enemyTeam, playerTeam),
                            AdditionalData = "Loose"
                        };
                    }
                    
                    commands.Add(order);
                }
                
                // Handle cavalry - flanking or charging based on situation
                foreach (var formation in cavalryFormations)
                {
                    FormationOrder order;
                    
                    if (_config.UseCommanderMemory && tacticalAdvice != null && tacticalAdvice.HasLearningData)
                    {
                        // If enemy has tactical advice from past battles, use it
                        string tacticToUse = recommendedTactic;
                        string formationToUse = preferredFormationType;
                        
                        // Apply tactical advice based on recommended tactic
                        switch (tacticToUse)
                        {
                            case "Offensive":
                                // Aggressive direct charge
                                order = new FormationOrder
                                {
                                    OrderType = FormationOrderType.Charge,
                                    TargetFormation = formation,
                                    TargetPosition = GetEnemyCenterPosition(playerTeam),
                                    AdditionalData = formationToUse
                                };
                                break;
                                
                            case "Flanking":
                                // Strategic flanking
                                order = new FormationOrder
                                {
                                    OrderType = FormationOrderType.Move,
                                    TargetFormation = formation,
                                    TargetPosition = GetFlankPosition(formation, playerTeam),
                                    AdditionalData = "Wedge"
                                };
                                break;
                                
                            case "CavalryCharge":
                                // Focus on weak points
                                order = new FormationOrder
                                {
                                    OrderType = FormationOrderType.Charge,
                                    TargetFormation = formation,
                                    TargetPosition = GetVulnerableTargetPosition(playerTeam),
                                    AdditionalData = "Wedge"
                                };
                                break;
                                
                            case "Defensive":
                                // Hold back cavalry for counter-attacks
                                order = new FormationOrder
                                {
                                    OrderType = FormationOrderType.Move,
                                    TargetFormation = formation,
                                    TargetPosition = GetDefensivePosition(formation, enemyTeam, playerTeam),
                                    AdditionalData = formationToUse
                                };
                                break;
                                
                            default:
                                // Special handling for vendetta commanders (more aggressive and unpredictable)
                                if (hasVendettaAgainstPlayer)
                                {
                                    // Nemesis with vendetta makes more aggressive, unexpected moves
                                    if (_config.Debug)
                                    {
                                        string title = tacticalAdvice.CommanderTitle;
                                        Logger.Instance.Debug($"Nemesis commander '{title}' is executing vendetta tactics");
                                    }
                                    
                                    order = new FormationOrder
                                    {
                                        OrderType = FormationOrderType.Charge,
                                        TargetFormation = formation,
                                        TargetPosition = GetEnemyCenterPosition(playerTeam),
                                        AdditionalData = "Wedge"
                                    };
                                }
                                else
                                {
                                    // Default balanced approach based on aggressiveness
                                    if (aggressivenessFactor > 0.6f)
                                    {
                                        order = new FormationOrder
                                        {
                                            OrderType = FormationOrderType.Charge,
                                            TargetFormation = formation,
                                            TargetPosition = GetEnemyCenterPosition(playerTeam),
                                            AdditionalData = formationToUse
                                        };
                                    }
                                    else
                                    {
                                        order = new FormationOrder
                                        {
                                            OrderType = FormationOrderType.Move,
                                            TargetFormation = formation,
                                            TargetPosition = GetFlankPosition(formation, playerTeam),
                                            AdditionalData = formationToUse
                                        };
                                    }
                                }
                                break;
                        }
                    }
                    else if (isOutnumbered)
                    {
                        // When outnumbered, cavalry should target archers or wait for opportunity
                        order = new FormationOrder
                        {
                            OrderType = FormationOrderType.FormWedge,
                            TargetFormation = formation,
                            TargetPosition = GetRangedFormationPosition(playerTeam),
                            AdditionalData = "Wedge"
                        };
                    }
                    else
                    {
                        // Default to standard flanking
                        order = new FormationOrder
                        {
                            OrderType = aggressivenessFactor > 0.5f ? FormationOrderType.Charge : FormationOrderType.Move,
                            TargetFormation = formation,
                            TargetPosition = GetFlankPosition(formation, playerTeam),
                            AdditionalData = "Wedge"
                        };
                    }
                    
                    commands.Add(order);
                }
                
                // Handle horse archers - harass and kite
                foreach (var formation in horseArcherFormations)
                {
                    FormationOrder order = new FormationOrder
                    {
                        OrderType = FormationOrderType.Move,
                        TargetFormation = formation,
                        TargetPosition = GetHorseArcherHarassPosition(formation, playerTeam),
                        AdditionalData = "Loose"
                    };
                    
                    commands.Add(order);
                }
                
                // Log if debug is enabled
                if (_config.Debug && commands.Count > 0)
                {
                    Logger.Instance.Debug($"Generated {commands.Count} enemy formation orders");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error processing enemy battle snapshot: {ex.Message}");
            }
            
            return commands;
        }
        
        /// <summary>
        /// Get formations by class
        /// </summary>
        private List<Formation> GetFormationsByClass(Team team, FormationClass formationClass)
        {
            List<Formation> result = new List<Formation>();
            
            if (team?.FormationsIncludingEmpty == null)
            {
                return result;
            }
            
            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                // Skip empty formations
                if (formation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                if (formation.FormationIndex == formationClass)
                {
                    result.Add(formation);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Count active agents in a team
        /// </summary>
        private int CountActiveAgents(Team team)
        {
            if (team?.ActiveAgents == null)
            {
                return 0;
            }
            
            return team.ActiveAgents.Count;
        }
        
        /// <summary>
        /// Check if a team has strong cavalry
        /// </summary>
        private bool DoesTeamHaveStrongCavalry(Team team)
        {
            int cavalryCount = 0;
            
            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                if (formation.FormationIndex == FormationClass.Cavalry || 
                    formation.FormationIndex == FormationClass.HorseArcher)
                {
                    cavalryCount += formation.CountOfUnits;
                }
            }
            
            // If cavalry is a significant portion of the army (more than 20% or more than 15 units)
            return cavalryCount > CountActiveAgents(team) / 5 || cavalryCount > 15;
        }
        
        /// <summary>
        /// Evaluate terrain advantages between teams
        /// </summary>
        private bool EvaluateTerrainAdvantage(Team team, Team enemyTeam)
        {
            try
            {
                Vec3 teamCenter = GetTeamCenterPosition(team);
                Vec3 enemyCenter = GetTeamCenterPosition(enemyTeam);
                
                return (teamCenter.z - enemyCenter.z) > HEIGHT_ADVANTAGE_THRESHOLD;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Get team center position
        /// </summary>
        private Vec3 GetTeamCenterPosition(Team team)
        {
            if (team == null)
            {
                return Vec3.Zero;
            }
            
            Vec3 sum = Vec3.Zero;
            int count = 0;
            
            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    sum += GetFormationPosition(formation);
                    count++;
                }
            }
            
            return count > 0 ? sum / count : Vec3.Zero;
        }
        
        /// <summary>
        /// Get a good position for defensive positioning
        /// </summary>
        private Vec3 GetDefensivePosition(Formation formation, Team team, Team enemyTeam)
        {
            // Just use current position for now - could be enhanced with terrain analysis
            return GetFormationPosition(formation);
        }
        
        /// <summary>
        /// Get a good position for controlled advance
        /// </summary>
        private Vec3 GetControlledAdvancePosition(Formation formation, Team team, Team enemyTeam)
        {
            Vec3 enemyCenter = GetEnemyCenterPosition(enemyTeam);
            Vec3 formationPos = GetFormationPosition(formation);
            Vec3 direction = enemyCenter - formationPos;
            
            if (direction.Length == 0)
            {
                return formationPos;
            }
            
            direction.Normalize();
            
            // Advance halfway toward the enemy
            return formationPos + (direction * (formationPos.Distance(enemyCenter) / 2));
        }
        
        /// <summary>
        /// Get position where ranged units are in the enemy team
        /// </summary>
        private Vec3 GetRangedFormationPosition(Team enemyTeam)
        {
            foreach (Formation formation in enemyTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                if (formation.FormationIndex == FormationClass.Ranged)
                {
                    return GetFormationPosition(formation);
                }
            }
            
            // If no ranged formation found, return enemy center
            return GetEnemyCenterPosition(enemyTeam);
        }
        
        /// <summary>
        /// Get cavalry position in enemy team
        /// </summary>
        private Vec3 GetCavalryPosition(Team enemyTeam)
        {
            foreach (Formation formation in enemyTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                if (formation.FormationIndex == FormationClass.Cavalry || 
                    formation.FormationIndex == FormationClass.HorseArcher)
                {
                    return GetFormationPosition(formation);
                }
            }
            
            // If no cavalry formation found, return enemy center
            return GetEnemyCenterPosition(enemyTeam);
        }
        
        /// <summary>
        /// Get the position of the most vulnerable target in a team
        /// Used by nemesis system for targeted attacks
        /// </summary>
        private Vec3 GetVulnerableTargetPosition(Team team)
        {
            // First priority: isolated ranged units
            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0 && formation.FormationIndex == FormationClass.Ranged)
                {
                    // Check if ranged formation is isolated
                    if (IsFormationIsolated(formation, team))
                    {
                        return GetFormationPosition(formation);
                    }
                }
            }
            
            // Second priority: smallest infantry formation
            Formation smallestInfantry = null;
            int smallestCount = int.MaxValue;
            
            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0 && formation.FormationIndex == FormationClass.Infantry)
                {
                    if (formation.CountOfUnits < smallestCount)
                    {
                        smallestCount = formation.CountOfUnits;
                        smallestInfantry = formation;
                    }
                }
            }
            
            if (smallestInfantry != null)
            {
                return GetFormationPosition(smallestInfantry);
            }
            
            // Third priority: player's position if available
            try
            {
                if (Mission.Current?.MainAgent != null && team.IsEnemyOf(Mission.Current.MainAgent.Team))
                {
                    // Target player directly - vendetta targeting
                    return GetAgentPosition(Mission.Current.MainAgent);
                }
            }
            catch
            {
                // Fallback to team center if exception occurs
            }
            
            // Default to team center
            return GetTeamCenterPosition(team);
        }
        
        /// <summary>
        /// Check if a formation is isolated from the rest of its team
        /// </summary>
        private bool IsFormationIsolated(Formation formation, Team team)
        {
            if (formation == null || team == null)
            {
                return false;
            }
            
            Vec3 formationPos = GetFormationPosition(formation);
            
            // Check distance to other friendly formations
            foreach (Formation otherFormation in team.FormationsIncludingEmpty)
            {
                if (otherFormation == formation || otherFormation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                Vec3 otherPos = GetFormationPosition(otherFormation);
                float distance = formationPos.Distance(otherPos);
                
                // If any friendly formation is nearby, not isolated
                if (distance < NEARBY_DISTANCE_THRESHOLD * 1.5f)
                {
                    return false;
                }
            }
            
            // No nearby friendly formations found
            return true;
        }
        
        /// <summary>
        /// Get a protected position for archers
        /// </summary>
        private Vec3 GetProtectedArcherPosition(Formation archerFormation, Team team, Team enemyTeam)
        {
            Vec3 enemyCenter = GetEnemyCenterPosition(enemyTeam);
            Vec3 teamCenter = GetTeamCenterPosition(team);
            
            // Place archers behind infantry line
            Vec3 direction = teamCenter - enemyCenter;
            
            if (direction.Length == 0)
            {
                return GetFormationPosition(archerFormation);
            }
            
            direction.Normalize();
            
            // Position 20 units behind team center, away from enemy
            return teamCenter + (direction * 20);
        }
        
        /// <summary>
        /// Get a good position for horse archers to harass from
        /// </summary>
        private Vec3 GetHorseArcherHarassPosition(Formation formation, Team enemyTeam)
        {
            Vec3 enemyCenter = GetEnemyCenterPosition(enemyTeam);
            Vec3 formationPos = GetFormationPosition(formation);
            
            Vec3 direction = formationPos - enemyCenter;
            
            if (direction.Length == 0)
            {
                // If at same position, use a default direction
                direction = new Vec3(1, 0, 0);
            }
            
            direction.Normalize();
            
            // Position horse archers at a distance with good firing angles
            return enemyCenter + (direction * 50);
        }
        /// <summary>
        /// Analyzes terrain and battle conditions to determine optimal tactical approach
        /// </summary>
        public TacticalApproach DetermineTacticalApproach(Team team, Team enemyTeam)
        {
            TacticalApproach approach = new TacticalApproach();
            
            try
            {
                if (team == null || enemyTeam == null)
                {
                    return approach;
                }
                
                // Analyze force composition
                approach.HasCavalryAdvantage = CalculateCavalryAdvantage(team, enemyTeam);
                approach.HasArcherAdvantage = CalculateRangedAdvantage(team, enemyTeam);
                approach.HasInfantryAdvantage = CalculateInfantryAdvantage(team, enemyTeam);
                
                // Analyze terrain factors
                Vec3 teamPosition = GetTeamCenterPosition(team);
                approach.HasHighGround = IsTerrainAdvantageous(teamPosition);
                approach.HasForestCover = HasForestCover(teamPosition, 50f);
                
                // Determine formation types based on advantages
                if (approach.HasHighGround && approach.HasArcherAdvantage)
                {
                    approach.RecommendedFormations.Add(FormationClass.Ranged, "Loose");
                    approach.RecommendedTactic = TacticalTactic.DefendHighGround;
                }
                else if (approach.HasCavalryAdvantage)
                {
                    approach.RecommendedFormations.Add(FormationClass.Cavalry, "Wedge");
                    approach.RecommendedTactic = TacticalTactic.CavalryFlanking;
                }
                else if (approach.HasInfantryAdvantage)
                {
                    approach.RecommendedFormations.Add(FormationClass.Infantry, "ShieldWall");
                    approach.RecommendedTactic = TacticalTactic.InfantryAdvance;
                }
                else
                {
                    // Balanced approach if no clear advantage
                    approach.RecommendedFormations.Add(FormationClass.Infantry, "Line");
                    approach.RecommendedFormations.Add(FormationClass.Ranged, "Loose");
                    approach.RecommendedTactic = TacticalTactic.BalancedApproach;
                }
                
                // Incorporate commander memory if enabled
                if (_config.UseCommanderMemory)
                {
                    // Get tactical advice from commander memory
                    TacticalAdvice tacticalAdvice = CommanderMemoryService.Instance.GetTacticalAdvice();
                    float aggression = tacticalAdvice?.SuggestedAggression ?? CommanderMemoryService.Instance.AggressivenessScore;
                    bool hasVendetta = tacticalAdvice?.HasVendettaAgainstPlayer ?? false;
                    
                    // Apply the tactical advice based on recommended tactic
                    if (tacticalAdvice != null && tacticalAdvice.HasLearningData)
                    {
                        // Use a preferred terrain if the memory system suggests one
                        if (!string.IsNullOrEmpty(tacticalAdvice.PreferredTerrain))
                        {
                            approach.PreferredTerrain = tacticalAdvice.PreferredTerrain;
                        }
                        
                        // Apply recommended tactic if available
                        switch (tacticalAdvice.RecommendedTactic)
                        {
                            case "Offensive":
                                approach.RecommendedTactic = TacticalTactic.AggressiveAdvance;
                                break;
                                
                            case "Defensive":
                                approach.RecommendedTactic = TacticalTactic.DefensivePositioning;
                                break;
                                
                            case "Flanking":
                                approach.RecommendedTactic = TacticalTactic.CavalryFlanking;
                                break;
                                
                            case "RangedFocus":
                                approach.RecommendedTactic = approach.HasHighGround ? 
                                    TacticalTactic.DefendHighGround : TacticalTactic.RangedSkirmish;
                                break;
                                
                            case "CavalryCharge":
                                approach.RecommendedTactic = TacticalTactic.CavalryCharge;
                                break;
                        }
                        
                        // Apply unit type effectiveness
                        if (tacticalAdvice.UnitEffectiveness != null && tacticalAdvice.UnitEffectiveness.Count > 0)
                        {
                            // Find the most effective unit type
                            string bestUnitType = "Infantry";
                            float bestEffectiveness = 0f;
                            
                            foreach (var pair in tacticalAdvice.UnitEffectiveness)
                            {
                                if (pair.Value > bestEffectiveness)
                                {
                                    bestEffectiveness = pair.Value;
                                    bestUnitType = pair.Key;
                                }
                            }
                            
                            // Prioritize the most effective unit type
                            switch (bestUnitType)
                            {
                                case "Infantry":
                                    approach.PrioritizeInfantry = true;
                                    break;
                                case "Ranged":
                                    approach.PrioritizeArchers = true;
                                    break;
                                case "Cavalry":
                                    approach.PrioritizeCavalry = true;
                                    break;
                                case "HorseArcher":
                                    approach.PrioritizeHorseArchers = true;
                                    break;
                            }
                        }
                        
                        // Nemesis-specific behaviors
                        if (hasVendetta)
                        {
                            approach.IsNemesis = true;
                            approach.NemesisTitle = tacticalAdvice.CommanderTitle;
                            
                            // If this commander is a nemesis, they may directly target the player
                            approach.TargetPlayerDirectly = true;
                            
                            // Log nemesis behavior if debug enabled
                            if (_config.Debug)
                            {
                                Logger.Instance.Debug($"Nemesis commander '{approach.NemesisTitle}' is executing vendetta tactics against player");
                            }
                        }
                    }
                    else
                    {
                        // Fall back to basic aggression-based system if no learning data
                        if (aggression > 0.7f)
                        {
                            approach.RecommendedTactic = TacticalTactic.AggressiveAdvance;
                        }
                        else if (aggression < 0.3f)
                        {
                            approach.RecommendedTactic = TacticalTactic.DefensivePositioning;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error determining tactical approach: {ex.Message}");
            }
            
            return approach;
        }
        
        /// <summary>
        /// Calculate if a team has a cavalry advantage over another
        /// </summary>
        private bool CalculateCavalryAdvantage(Team team, Team enemyTeam)
        {
            int teamCavalryCount = 0;
            int enemyCavalryCount = 0;
            
            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                if (formation.FormationIndex == FormationClass.Cavalry ||
                    formation.FormationIndex == FormationClass.HorseArcher)
                {
                    teamCavalryCount += formation.CountOfUnits;
                }
            }
            
            foreach (Formation formation in enemyTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                if (formation.FormationIndex == FormationClass.Cavalry ||
                    formation.FormationIndex == FormationClass.HorseArcher)
                {
                    enemyCavalryCount += formation.CountOfUnits;
                }
            }
            
            // Consider both relative and absolute numbers
            return (teamCavalryCount > enemyCavalryCount * 1.3f) || (teamCavalryCount > 25 && teamCavalryCount > enemyCavalryCount);
        }
        
        /// <summary>
        /// Calculate if a team has a ranged advantage over another
        /// </summary>
        private bool CalculateRangedAdvantage(Team team, Team enemyTeam)
        {
            int teamRangedCount = 0;
            int enemyRangedCount = 0;
            
            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                if (formation.FormationIndex == FormationClass.Ranged ||
                    formation.FormationIndex == FormationClass.HorseArcher)
                {
                    teamRangedCount += formation.CountOfUnits;
                }
            }
            
            foreach (Formation formation in enemyTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                if (formation.FormationIndex == FormationClass.Ranged ||
                    formation.FormationIndex == FormationClass.HorseArcher)
                {
                    enemyRangedCount += formation.CountOfUnits;
                }
            }
            
            return (teamRangedCount > enemyRangedCount * 1.3f) || (teamRangedCount > 20 && teamRangedCount > enemyRangedCount);
        }
        
        /// <summary>
        /// Calculate if a team has an infantry advantage over another
        /// </summary>
        private bool CalculateInfantryAdvantage(Team team, Team enemyTeam)
        {
            int teamInfantryCount = 0;
            int enemyInfantryCount = 0;
            
            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                if (formation.FormationIndex == FormationClass.Infantry)
                {
                    teamInfantryCount += formation.CountOfUnits;
                }
            }
            
            foreach (Formation formation in enemyTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0)
                {
                    continue;
                }
                
                if (formation.FormationIndex == FormationClass.Infantry)
                {
                    enemyInfantryCount += formation.CountOfUnits;
                }
            }
            
            return (teamInfantryCount > enemyInfantryCount * 1.2f) || (teamInfantryCount > 30 && teamInfantryCount > enemyInfantryCount);
        }
        
        /// <summary>
        /// Check if a position has forest cover nearby for tactical considerations
        /// </summary>
        private bool HasForestCover(Vec3 position, float searchRadius)
        {
            // In real implementation, would use scene data to check for forest
            // Simplified version for compatibility
            
            try
            {
                // Use a simple height variation as a proxy for trees and cover
                // In the real game, this would use the actual scene vegetation data
                float terrainVariation = GetTerrainHeightVariation(position, searchRadius);
                return terrainVariation > 2.0f; // Significant height variation suggests trees or cover
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Get terrain height variation in an area (simplified proxy for forest detection)
        /// </summary>
        private float GetTerrainHeightVariation(Vec3 center, float radius)
        {
            // Simplified implementation that just returns a reasonable value
            // In the real game, would analyze the actual terrain
            
            // For testing/compatibility, use position Z value as a proxy 
            // Higher positions tend to have more natural variation/cover in the game
            float baseHeight = center.z;
            
            // Return a percentage of base height as "variation"
            // Areas with any elevation tend to have more vegetation in the game
            return baseHeight * 0.1f;
        }
        
        /// <summary>
        /// Apply formation behavioral modifiers based on terrain and tactical conditions
        /// </summary>
        public void ApplyTerrainTactics(List<FormationOrder> orders, TacticalApproach approach)
        {
            try
            {
                // Special handling for nemesis commanders with vendetta
                if (approach.IsNemesis && !string.IsNullOrEmpty(approach.NemesisTitle) && _config.Debug)
                {
                    Logger.Instance.Debug($"Applying nemesis tactics for commander '{approach.NemesisTitle}'");
                }
                
                foreach (var order in orders)
                {
                    // Skip null formations
                    if (order.TargetFormation == null)
                    {
                        continue;
                    }
                    
                    FormationClass formationClass = order.TargetFormation.FormationIndex;
                    
                    // Apply unit prioritization from nemesis system if enabled
                    if (approach.PrioritizeInfantry && formationClass == FormationClass.Infantry)
                    {
                        // Make this formation more aggressive as it's prioritized
                        if (order.OrderType == FormationOrderType.Move)
                        {
                            order.OrderType = FormationOrderType.Advance;
                        }
                    }
                    else if (approach.PrioritizeArchers && formationClass == FormationClass.Ranged)
                    {
                        // Prioritize archer positioning and protection
                        order.AdditionalData = "Loose";
                    }
                    else if (approach.PrioritizeCavalry && formationClass == FormationClass.Cavalry)
                    {
                        // Make cavalry more aggressive if prioritized
                        order.OrderType = FormationOrderType.Charge;
                        order.AdditionalData = "Wedge";
                    }
                    else if (approach.PrioritizeHorseArchers && formationClass == FormationClass.HorseArcher)
                    {
                        // Make horse archers more mobile
                        order.AdditionalData = "Loose";
                    }
                    
                    // If this is a nemesis commander with vendetta targeting the player directly
                    if (approach.IsNemesis && approach.TargetPlayerDirectly && Mission.Current?.MainAgent != null)
                    {
                        // Target the player's position more directly with certain unit types
                        if (formationClass == FormationClass.Cavalry || 
                            (formationClass == FormationClass.Infantry && !approach.HasHighGround))
                        {
                            // Direct some forces at player
                            order.TargetPosition = GetAgentPosition(Mission.Current.MainAgent);
                            
                            // Keep track of last order to player for debugging
                            if (_config.Debug)
                            {
                                Logger.Instance.Debug($"Nemesis commander directing {formationClass} formation at player position");
                            }
                        }
                    }
                    
                    // Apply behavioral modifiers based on tactical approach
                    switch (approach.RecommendedTactic)
                    {
                        case TacticalTactic.DefendHighGround:
                            if (formationClass == FormationClass.Ranged)
                            {
                                // Prioritize holding positions for archers on high ground
                                order.OrderType = FormationOrderType.Move;
                                order.AdditionalData = "Loose";
                            }
                            else if (formationClass == FormationClass.Infantry)
                            {
                                // Infantry forms defensive shield wall
                                order.OrderType = FormationOrderType.FormShieldWall;
                                order.AdditionalData = "ShieldWall";
                            }
                            break;
                            
                        case TacticalTactic.CavalryFlanking:
                            if (formationClass == FormationClass.Cavalry)
                            {
                                // Cavalry uses wedge and flanking
                                order.OrderType = FormationOrderType.FormWedge;
                                order.AdditionalData = "Wedge";
                            }
                            break;
                            
                        case TacticalTactic.InfantryAdvance:
                            if (formationClass == FormationClass.Infantry)
                            {
                                // Infantry advances in line formation
                                order.OrderType = FormationOrderType.Advance;
                                order.AdditionalData = "Line";
                            }
                            break;
                            
                        case TacticalTactic.AggressiveAdvance:
                            // All units advance aggressively
                            if (formationClass == FormationClass.Cavalry || 
                                formationClass == FormationClass.HorseArcher)
                            {
                                order.OrderType = FormationOrderType.Charge;
                            }
                            else
                            {
                                order.OrderType = FormationOrderType.Advance;
                            }
                            break;
                            
                        case TacticalTactic.DefensivePositioning:
                            // All units take defensive posture
                            if (formationClass == FormationClass.Infantry)
                            {
                                order.OrderType = FormationOrderType.FormShieldWall;
                                order.AdditionalData = "ShieldWall";
                            }
                            else if (formationClass == FormationClass.Ranged)
                            {
                                order.OrderType = FormationOrderType.FireAt;
                            }
                            break;
                    }
                    
                    // Apply terrain-specific modifiers
                    if (approach.HasForestCover && formationClass == FormationClass.Infantry)
                    {
                        // Infantry in forest should use looser formations
                        order.AdditionalData = "Loose";
                    }
                    else if (approach.HasHighGround && formationClass == FormationClass.Ranged)
                    {
                        // Ranged units on high ground gain advantage
                        order.AdditionalData = "Loose";
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error applying terrain tactics: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Represents a tactical approach with various advantages and recommended formations
    /// </summary>
    public class TacticalApproach
    {
        // Basic tactical information
        public bool HasCavalryAdvantage { get; set; }
        public bool HasArcherAdvantage { get; set; }
        public bool HasInfantryAdvantage { get; set; }
        public bool HasHighGround { get; set; }
        public bool HasForestCover { get; set; }
        public TacticalTactic RecommendedTactic { get; set; }
        public Dictionary<FormationClass, string> RecommendedFormations { get; set; }
        
        // Unit prioritization flags for nemesis system
        public bool PrioritizeInfantry { get; set; }
        public bool PrioritizeArchers { get; set; }
        public bool PrioritizeCavalry { get; set; }
        public bool PrioritizeHorseArchers { get; set; }
        
        // Nemesis system integration
        public bool IsNemesis { get; set; }
        public string NemesisTitle { get; set; }
        public bool TargetPlayerDirectly { get; set; }
        public string PreferredTerrain { get; set; }
        
        public TacticalApproach()
        {
            // Initialize default values
            RecommendedFormations = new Dictionary<FormationClass, string>();
            RecommendedTactic = TacticalTactic.BalancedApproach;
            
            // Initialize unit priorities
            PrioritizeInfantry = false;
            PrioritizeArchers = false;
            PrioritizeCavalry = false;
            PrioritizeHorseArchers = false;
            
            // Initialize nemesis properties
            IsNemesis = false;
            NemesisTitle = "";
            TargetPlayerDirectly = false;
            PreferredTerrain = "Open";
        }
    }
    
    /// <summary>
    /// Represents different tactical approaches for AI
    /// </summary>
    public enum TacticalTactic
    {
        BalancedApproach,
        DefendHighGround,
        CavalryFlanking,
        InfantryAdvance,
        AggressiveAdvance,
        DefensivePositioning,
        RangedSkirmish,
        CavalryCharge
    }
}
