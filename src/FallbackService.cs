using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HannibalAI
{
    /// <summary>
    /// Service to manage fallback decisions when primary AI logic fails
    /// </summary>
    public class FallbackService
    {
        private readonly ModConfig _config;
        private readonly AIService _aiService;
        
        public FallbackService(ModConfig config, AIService aiService)
        {
            _config = config;
            _aiService = aiService;
        }
        
        /// <summary>
        /// Generate fallback formation orders when primary decision making fails
        /// </summary>
        public List<FormationOrder> GenerateFallbackOrders(Team team, Team enemyTeam)
        {
            List<FormationOrder> fallbackOrders = new List<FormationOrder>();
            
            try
            {
                if (team == null || team.FormationsIncludingEmpty == null || team.FormationsIncludingEmpty.Count == 0)
                {
                    return fallbackOrders;
                }
                
                // Determine team situation
                bool isDefensive = ShouldDefend(team, enemyTeam);
                
                // Get various formation types
                List<Formation> infantryFormations = GetFormationsByClass(team, FormationClass.Infantry);
                List<Formation> rangedFormations = GetFormationsByClass(team, FormationClass.Ranged);
                List<Formation> cavalryFormations = GetFormationsByClass(team, FormationClass.Cavalry);
                
                // Apply nemesis system influence if enabled
                bool preferFlankingOverCharge = false;
                bool preferDefensivePositioning = false;
                
                if (_config.UseCommanderMemory && CommanderMemoryService.Instance.TimesDefeatedPlayer >= 3)
                {
                    // Commander who has defeated player 3+ times adapts tactics
                    preferFlankingOverCharge = true;
                    
                    // Commander with vendetta is more aggressive
                    if (CommanderMemoryService.Instance.HasVendettaAgainstPlayer)
                    {
                        isDefensive = false;
                    }
                }
                
                // Less aggressive commanders prefer defensive positioning
                if (_config.UseCommanderMemory && CommanderMemoryService.Instance.AggressivenessScore < 0.4f)
                {
                    preferDefensivePositioning = true;
                    isDefensive = true;
                }
                
                // Generate basic orders based on formation type
                if (isDefensive)
                {
                    GenerateDefensiveFallbackOrders(fallbackOrders, infantryFormations, rangedFormations, cavalryFormations);
                }
                else
                {
                    GenerateOffensiveFallbackOrders(fallbackOrders, infantryFormations, rangedFormations, cavalryFormations, preferFlankingOverCharge);
                }
                
                // If we have a preferred formation from memory, use it
                if (_config.UseCommanderMemory && !string.IsNullOrEmpty(CommanderMemoryService.Instance.PreferredFormation))
                {
                    ApplyPreferredFormation(fallbackOrders, CommanderMemoryService.Instance.PreferredFormation);
                }
                
                // Log fallback scenario
                if (_config.Debug)
                {
                    _aiService.LogInfo("Using fallback orders: " + (isDefensive ? "Defensive" : "Offensive"));
                }
            }
            catch (Exception ex)
            {
                _aiService.LogError($"Error generating fallback orders: {ex.Message}");
            }
            
            return fallbackOrders;
        }
        
        /// <summary>
        /// Determine if team should use defensive tactics
        /// </summary>
        private bool ShouldDefend(Team team, Team enemyTeam)
        {
            if (team == null || enemyTeam == null)
            {
                return true; // Default to defensive if missing data
            }
            
            // Check unit count advantage
            int teamCount = CountActiveAgents(team);
            int enemyCount = CountActiveAgents(enemyTeam);
            
            // Default defensiveness check
            bool isOutnumbered = teamCount < enemyCount;
            
            // Apply commander memory if enabled
            if (_config.UseCommanderMemory)
            {
                // More aggressive commanders are less likely to defend
                float aggressionModifier = CommanderMemoryService.Instance.AggressivenessScore;
                
                // If significantly aggressive, might attack even when outnumbered
                if (aggressionModifier > 0.7f)
                {
                    isOutnumbered = teamCount < enemyCount * 1.3f;
                }
                // If very cautious, more likely to defend
                else if (aggressionModifier < 0.3f)
                {
                    isOutnumbered = teamCount < enemyCount * 0.8f;
                }
            }
            
            return isOutnumbered;
        }
        
        /// <summary>
        /// Generate defensive fallback orders
        /// </summary>
        private void GenerateDefensiveFallbackOrders(
            List<FormationOrder> orders,
            List<Formation> infantryFormations,
            List<Formation> rangedFormations,
            List<Formation> cavalryFormations)
        {
            // Infantry forms shield wall in front
            foreach (var formation in infantryFormations)
            {
                orders.Add(new FormationOrder
                {
                    OrderType = FormationOrderType.FormShieldWall,
                    TargetFormation = formation,
                    TargetPosition = Vec3.Zero, // Current position
                    AdditionalData = "ShieldWall"
                });
            }
            
            // Ranged units in loose formation behind infantry
            foreach (var formation in rangedFormations)
            {
                orders.Add(new FormationOrder
                {
                    OrderType = FormationOrderType.FormLoose,
                    TargetFormation = formation,
                    TargetPosition = Vec3.Zero, // Current position
                    AdditionalData = "Loose"
                });
            }
            
            // Cavalry in reserve on the flanks
            foreach (var formation in cavalryFormations)
            {
                orders.Add(new FormationOrder
                {
                    OrderType = FormationOrderType.Move,
                    TargetFormation = formation,
                    TargetPosition = Vec3.Zero, // Current position
                    AdditionalData = "Column"
                });
            }
        }
        
        /// <summary>
        /// Generate offensive fallback orders
        /// </summary>
        private void GenerateOffensiveFallbackOrders(
            List<FormationOrder> orders,
            List<Formation> infantryFormations,
            List<Formation> rangedFormations,
            List<Formation> cavalryFormations,
            bool preferFlankingOverCharge)
        {
            // Infantry advances in line formation
            foreach (var formation in infantryFormations)
            {
                orders.Add(new FormationOrder
                {
                    OrderType = FormationOrderType.Advance,
                    TargetFormation = formation,
                    TargetPosition = Vec3.Zero,
                    AdditionalData = "Line"
                });
            }
            
            // Ranged units provide covering fire
            foreach (var formation in rangedFormations)
            {
                orders.Add(new FormationOrder
                {
                    OrderType = FormationOrderType.FormLoose,
                    TargetFormation = formation,
                    TargetPosition = Vec3.Zero,
                    AdditionalData = "Loose"
                });
            }
            
            // Cavalry charges or flanks based on strategy
            foreach (var formation in cavalryFormations)
            {
                if (preferFlankingOverCharge)
                {
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.FormWedge,
                        TargetFormation = formation,
                        TargetPosition = Vec3.Zero,
                        AdditionalData = "Wedge"
                    });
                }
                else
                {
                    orders.Add(new FormationOrder
                    {
                        OrderType = FormationOrderType.Charge,
                        TargetFormation = formation,
                        TargetPosition = Vec3.Zero,
                        AdditionalData = null
                    });
                }
            }
        }
        
        /// <summary>
        /// Apply preferred formation from commander memory
        /// </summary>
        private void ApplyPreferredFormation(List<FormationOrder> orders, string preferredFormationType)
        {
            foreach (var order in orders)
            {
                // Only override formation type for move and advance orders
                if (order.OrderType == FormationOrderType.Move || order.OrderType == FormationOrderType.Advance)
                {
                    order.AdditionalData = preferredFormationType;
                }
            }
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
    }
}