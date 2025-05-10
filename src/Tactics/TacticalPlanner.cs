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
    /// Tactical planner that makes high-level strategic decisions for formations
    /// and coordinates actions between multiple formations
    /// </summary>
    public class TacticalPlanner
    {
        private static TacticalPlanner _instance;
        
        public static TacticalPlanner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TacticalPlanner();
                }
                return _instance;
            }
        }
        
        // Current tactical plans
        private Dictionary<Team, TacticalPlan> _teamPlans;
        
        // Battle context
        private Dictionary<Team, BattlefieldAssessment> _battlefieldAssessments;
        
        // Time tracking
        private DateTime _lastPlanningTime;
        private const float PLANNING_INTERVAL = 5.0f; // Seconds between major plan updates
        
        // Combined arms coordination
        private Dictionary<FormationClass, List<FormationAction>> _pendingActions;
        
        // References to other services
        private FormationController _formationController;
        private TerrainAnalyzer _terrainAnalyzer;
        
        public TacticalPlanner()
        {
            _teamPlans = new Dictionary<Team, TacticalPlan>();
            _battlefieldAssessments = new Dictionary<Team, BattlefieldAssessment>();
            _pendingActions = new Dictionary<FormationClass, List<FormationAction>>();
            _lastPlanningTime = DateTime.Now;
            
            // Initialize controllers
            _formationController = FormationController.Instance;
            _terrainAnalyzer = TerrainAnalyzer.Instance;
            
            // Initialize pending actions for each formation class
            foreach (FormationClass formationClass in Enum.GetValues(typeof(FormationClass)))
            {
                if (formationClass != FormationClass.NumberOfAllFormations)
                {
                    _pendingActions[formationClass] = new List<FormationAction>();
                }
            }
            
            Logger.Instance.Info("TacticalPlanner initialized");
        }
        
        /// <summary>
        /// Create or update a tactical plan for a team
        /// </summary>
        public TacticalPlan DevelopTacticalPlan(Team team, Team enemyTeam)
        {
            try
            {
                // Check if it's time for a new plan
                bool needsNewPlan = !_teamPlans.ContainsKey(team) || 
                                    (DateTime.Now - _lastPlanningTime).TotalSeconds > PLANNING_INTERVAL;
                
                // Use existing plan if it's still valid and it's not time for an update
                if (!needsNewPlan && _teamPlans.ContainsKey(team))
                {
                    return _teamPlans[team];
                }
                
                // 1. Assess battlefield situation
                BattlefieldAssessment assessment = AssessBattlefield(team, enemyTeam);
                _battlefieldAssessments[team] = assessment;
                
                // 2. Create a new tactical plan
                TacticalPlan plan = new TacticalPlan();
                
                // 3. Set overall strategy based on assessment
                plan.Strategy = DetermineOverallStrategy(assessment);
                
                // 4. Assign roles to formations based on strategy
                AssignFormationRoles(plan, team, assessment);
                
                // 5. Create actions for each formation
                CreateFormationActions(plan, team, enemyTeam, assessment);
                
                // Store the plan
                _teamPlans[team] = plan;
                _lastPlanningTime = DateTime.Now;
                
                // Log the plan
                Logger.Instance.Info($"Developed tactical plan: {plan.Strategy} with {plan.FormationRoles.Count} formation roles");
                
                return plan;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error developing tactical plan: {ex.Message}");
                
                // Return empty plan if error
                return new TacticalPlan();
            }
        }
        
        /// <summary>
        /// Assess the current battlefield situation
        /// </summary>
        private BattlefieldAssessment AssessBattlefield(Team team, Team enemyTeam)
        {
            BattlefieldAssessment assessment = new BattlefieldAssessment();
            
            try
            {
                // 1. Analyze terrain features - Simplified implementation for compatibility
                Logger.Instance.Info("Analyzing terrain features");
                var terrainFeatures = TerrainAnalyzer.Instance.AnalyzeCurrentTerrain();
                // Use a default terrain type
                assessment.TerrainType = HannibalAI.Terrain.TerrainType.Plains;
                
                // 2. Check for tactical features - Simplified implementation for compatibility
                assessment.HighGroundPositions = new List<Vec3>(); // Would use TerrainAnalyzer.Instance.GetTerrainFeaturesByType
                assessment.ChokepointPositions = new List<Vec3>(); // Would use TerrainAnalyzer.Instance.GetTerrainFeaturesByType
                assessment.HasHighGround = false; // Would be determined by actual terrain analysis
                assessment.HasChokepoints = false; // Would be determined by actual terrain analysis
                
                // 3. Assess force composition and strength
                AssessForceComposition(team, enemyTeam, assessment);
                
                // 4. Determine advantageous formations based on composition
                DetermineFormationAdvantages(assessment);
                
                // 5. Get commander personality influence
                assessment.AggressionLevel = CommanderMemoryService.Instance.AggressivenessScore;
                assessment.HasVendettaAgainstPlayer = CommanderMemoryService.Instance.HasVendettaAgainstPlayer;
                
                // 6. Get player analysis if this is enemy AI
                bool isPlayerTeam = IsPlayerInTeam(team);
                assessment.IsPlayerTeam = isPlayerTeam;
                assessment.IsAgainstPlayer = !isPlayerTeam && IsPlayerInTeam(enemyTeam);
                
                // 7. Calculate overall situational assessment
                assessment.StrengthRatio = (float)assessment.OurTotalTroops / Math.Max(1, assessment.EnemyTotalTroops);
                assessment.HasOverallAdvantage = assessment.StrengthRatio > 1.2f;
                assessment.HasOverallDisadvantage = assessment.StrengthRatio < 0.8f;
                
                Logger.Instance.Info($"Battlefield assessment: " +
                    $"Terrain={assessment.TerrainType}, " +
                    $"StrengthRatio={assessment.StrengthRatio:F2}, " +
                    $"Aggression={assessment.AggressionLevel:F2}, " +
                    $"HasHighGround={assessment.HasHighGround}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error assessing battlefield: {ex.Message}");
            }
            
            return assessment;
        }
        
        /// <summary>
        /// Assess force composition for both teams
        /// </summary>
        private void AssessForceComposition(Team team, Team enemyTeam, BattlefieldAssessment assessment)
        {
            try
            {
                // Count different unit types
                int ourInfantry = 0;
                int ourArchers = 0;
                int ourCavalry = 0;
                int ourHorseArchers = 0;
                
                int enemyInfantry = 0;
                int enemyArchers = 0;
                int enemyCavalry = 0;
                int enemyHorseArchers = 0;
                
                // Analyze our team
                foreach (Formation formation in team.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits <= 0) continue;
                    
                    switch (formation.FormationIndex)
                    {
                        case FormationClass.Infantry:
                            ourInfantry += formation.CountOfUnits;
                            break;
                        case FormationClass.Ranged:
                            ourArchers += formation.CountOfUnits;
                            break;
                        case FormationClass.Cavalry:
                            ourCavalry += formation.CountOfUnits;
                            break;
                        case FormationClass.HorseArcher:
                            ourHorseArchers += formation.CountOfUnits;
                            break;
                    }
                }
                
                // Analyze enemy team
                foreach (Formation formation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits <= 0) continue;
                    
                    switch (formation.FormationIndex)
                    {
                        case FormationClass.Infantry:
                            enemyInfantry += formation.CountOfUnits;
                            break;
                        case FormationClass.Ranged:
                            enemyArchers += formation.CountOfUnits;
                            break;
                        case FormationClass.Cavalry:
                            enemyCavalry += formation.CountOfUnits;
                            break;
                        case FormationClass.HorseArcher:
                            enemyHorseArchers += formation.CountOfUnits;
                            break;
                    }
                }
                
                // Store the counts
                assessment.OurInfantryCount = ourInfantry;
                assessment.OurArcherCount = ourArchers;
                assessment.OurCavalryCount = ourCavalry;
                assessment.OurHorseArcherCount = ourHorseArchers;
                assessment.OurTotalTroops = ourInfantry + ourArchers + ourCavalry + ourHorseArchers;
                
                assessment.EnemyInfantryCount = enemyInfantry;
                assessment.EnemyArcherCount = enemyArchers;
                assessment.EnemyCavalryCount = enemyCavalry;
                assessment.EnemyHorseArcherCount = enemyHorseArchers;
                assessment.EnemyTotalTroops = enemyInfantry + enemyArchers + enemyCavalry + enemyHorseArchers;
                
                // Calculate relative strengths
                float ourInfantryRatio = assessment.OurTotalTroops > 0 ? (float)ourInfantry / assessment.OurTotalTroops : 0;
                float ourArcherRatio = assessment.OurTotalTroops > 0 ? (float)ourArchers / assessment.OurTotalTroops : 0;
                float ourCavalryRatio = assessment.OurTotalTroops > 0 ? (float)ourCavalry / assessment.OurTotalTroops : 0;
                float ourHorseArcherRatio = assessment.OurTotalTroops > 0 ? (float)ourHorseArchers / assessment.OurTotalTroops : 0;
                
                float enemyInfantryRatio = assessment.EnemyTotalTroops > 0 ? (float)enemyInfantry / assessment.EnemyTotalTroops : 0;
                float enemyArcherRatio = assessment.EnemyTotalTroops > 0 ? (float)enemyArchers / assessment.EnemyTotalTroops : 0;
                float enemyCavalryRatio = assessment.EnemyTotalTroops > 0 ? (float)enemyCavalry / assessment.EnemyTotalTroops : 0;
                float enemyHorseArcherRatio = assessment.EnemyTotalTroops > 0 ? (float)enemyHorseArchers / assessment.EnemyTotalTroops : 0;
                
                // Store composition ratios
                assessment.OurInfantryRatio = ourInfantryRatio;
                assessment.OurArcherRatio = ourArcherRatio;
                assessment.OurCavalryRatio = ourCavalryRatio;
                assessment.OurHorseArcherRatio = ourHorseArcherRatio;
                
                assessment.EnemyInfantryRatio = enemyInfantryRatio;
                assessment.EnemyArcherRatio = enemyArcherRatio;
                assessment.EnemyCavalryRatio = enemyCavalryRatio;
                assessment.EnemyHorseArcherRatio = enemyHorseArcherRatio;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error assessing force composition: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Determine which formations have advantages based on composition
        /// </summary>
        private void DetermineFormationAdvantages(BattlefieldAssessment assessment)
        {
            try
            {
                // Infantry advantage if we have proportionally more infantry or enemy has many archers
                assessment.HasInfantryAdvantage = 
                    (assessment.OurInfantryRatio > assessment.EnemyInfantryRatio * 1.2f) ||
                    (assessment.EnemyArcherRatio > 0.4f && assessment.OurInfantryRatio > 0.3f);
                
                // Archer advantage if we have many archers and enemy has few cavalry
                assessment.HasArcherAdvantage = 
                    (assessment.OurArcherRatio > assessment.EnemyArcherRatio * 1.2f) &&
                    (assessment.EnemyCavalryRatio < 0.3f);
                
                // Cavalry advantage if we have cavalry and enemy has archers or few spearmen
                assessment.HasCavalryAdvantage = 
                    (assessment.OurCavalryRatio > assessment.EnemyCavalryRatio * 1.2f) &&
                    (assessment.EnemyArcherRatio > 0.3f || assessment.EnemyInfantryRatio < 0.3f);
                
                // Horse archer advantage if we have them and enemy lacks cavalry
                assessment.HasHorseArcherAdvantage = 
                    (assessment.OurHorseArcherRatio > 0.2f) &&
                    (assessment.EnemyCavalryRatio < 0.2f);
                
                // Overall composition advantage
                assessment.HasCompositionAdvantage = 
                    assessment.HasInfantryAdvantage || 
                    assessment.HasArcherAdvantage || 
                    assessment.HasCavalryAdvantage || 
                    assessment.HasHorseArcherAdvantage;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error determining formation advantages: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Determine the overall strategy based on battlefield assessment
        /// </summary>
        private TacticalStrategy DetermineOverallStrategy(BattlefieldAssessment assessment)
        {
            try
            {
                // Default strategy is balanced, we'll change based on conditions
                
                // If we have vendetta against player, be aggressive
                if (assessment.HasVendettaAgainstPlayer && assessment.IsAgainstPlayer)
                {
                    return TacticalStrategy.Aggressive;
                }
                
                // If we're significantly weaker, play defensive
                if (assessment.HasOverallDisadvantage)
                {
                    // If we also have high ground, prefer Defensive
                    if (assessment.HasHighGround)
                    {
                        return TacticalStrategy.Defensive;
                    }
                    // Otherwise retreat to better ground
                    else
                    {
                        return TacticalStrategy.Retreat;
                    }
                }
                
                // If we're significantly stronger, be aggressive
                if (assessment.HasOverallAdvantage && assessment.AggressionLevel > 0.4f)
                {
                    return TacticalStrategy.Aggressive;
                }
                
                // If we have composition advantage, use it
                if (assessment.HasCompositionAdvantage)
                {
                    // If we have cavalry advantage, try flanking
                    if (assessment.HasCavalryAdvantage || assessment.HasHorseArcherAdvantage)
                    {
                        return TacticalStrategy.Flanking;
                    }
                    // If we have archer advantage, try skirmishing
                    else if (assessment.HasArcherAdvantage)
                    {
                        return TacticalStrategy.Skirmish;
                    }
                    // If we have infantry advantage, be aggressive
                    else if (assessment.HasInfantryAdvantage)
                    {
                        return assessment.AggressionLevel > 0.5f ? 
                            TacticalStrategy.Aggressive : TacticalStrategy.Balanced;
                    }
                }
                
                // Terrain-based strategies
                if (assessment.HasHighGround)
                {
                    // Hold high ground if we have archers
                    if (assessment.OurArcherRatio > 0.2f)
                    {
                        return TacticalStrategy.Defensive;
                    }
                }
                
                if (assessment.HasChokepoints)
                {
                    // Use chokepoints if we have good infantry
                    if (assessment.OurInfantryRatio > 0.4f)
                    {
                        return TacticalStrategy.Defensive;
                    }
                }
                
                // Adjust based on aggression level
                if (assessment.AggressionLevel > 0.7f)
                {
                    return TacticalStrategy.Aggressive;
                }
                else if (assessment.AggressionLevel < 0.3f)
                {
                    return TacticalStrategy.Defensive;
                }
                
                // Default to balanced
                return TacticalStrategy.Balanced;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error determining strategy: {ex.Message}");
                return TacticalStrategy.Balanced;
            }
        }
        
        /// <summary>
        /// Assign tactical roles to each formation
        /// </summary>
        private void AssignFormationRoles(TacticalPlan plan, Team team, BattlefieldAssessment assessment)
        {
            try
            {
                // Clear existing roles
                plan.FormationRoles.Clear();
                
                // For each formation in the team
                foreach (Formation formation in team.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits <= 0) continue;
                    
                    // Create a role based on formation type and overall strategy
                    FormationRole role = new FormationRole();
                    role.Formation = formation;
                    
                    switch (formation.FormationIndex)
                    {
                        case FormationClass.Infantry:
                            role.Role = DetermineInfantryRole(formation, plan.Strategy, assessment);
                            break;
                        case FormationClass.Ranged:
                            role.Role = DetermineRangedRole(formation, plan.Strategy, assessment);
                            break;
                        case FormationClass.Cavalry:
                            role.Role = DetermineCavalryRole(formation, plan.Strategy, assessment);
                            break;
                        case FormationClass.HorseArcher:
                            role.Role = DetermineHorseArcherRole(formation, plan.Strategy, assessment);
                            break;
                    }
                    
                    // Add to plan
                    plan.FormationRoles.Add(role);
                    
                    // Log the role assignment
                    Logger.Instance.Info($"Assigned role {role.Role} to {formation.FormationIndex}");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error assigning formation roles: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Determine the role for an infantry formation
        /// </summary>
        private FormationRoleType DetermineInfantryRole(Formation formation, TacticalStrategy strategy, BattlefieldAssessment assessment)
        {
            switch (strategy)
            {
                case TacticalStrategy.Aggressive:
                    return FormationRoleType.Assault;
                    
                case TacticalStrategy.Defensive:
                    return FormationRoleType.HoldPosition;
                    
                case TacticalStrategy.Flanking:
                    return FormationRoleType.HoldCenter;
                    
                case TacticalStrategy.Skirmish:
                    return FormationRoleType.ScreenAdvance;
                    
                case TacticalStrategy.Retreat:
                    return FormationRoleType.RearGuard;
                    
                case TacticalStrategy.Balanced:
                default:
                    // More situational decision for balanced strategy
                    if (assessment.HasHighGround)
                    {
                        return FormationRoleType.HoldPosition;
                    }
                    else if (assessment.HasInfantryAdvantage)
                    {
                        return FormationRoleType.Assault;
                    }
                    else
                    {
                        return FormationRoleType.HoldCenter;
                    }
            }
        }
        
        /// <summary>
        /// Determine the role for a ranged formation
        /// </summary>
        private FormationRoleType DetermineRangedRole(Formation formation, TacticalStrategy strategy, BattlefieldAssessment assessment)
        {
            // Ranged units generally want to find good positions and fire
            switch (strategy)
            {
                case TacticalStrategy.Aggressive:
                    return FormationRoleType.FireSupport;
                    
                case TacticalStrategy.Defensive:
                    return FormationRoleType.FireAtWill;
                    
                case TacticalStrategy.Flanking:
                    return FormationRoleType.FireSupport;
                    
                case TacticalStrategy.Skirmish:
                    return FormationRoleType.HarassAndRetreat;
                    
                case TacticalStrategy.Retreat:
                    return FormationRoleType.CoveringFire;
                    
                case TacticalStrategy.Balanced:
                default:
                    // Position archers on high ground if available
                    if (assessment.HasHighGround)
                    {
                        return FormationRoleType.FireFromElevation;
                    }
                    else
                    {
                        return FormationRoleType.FireAtWill;
                    }
            }
        }
        
        /// <summary>
        /// Determine the role for a cavalry formation
        /// </summary>
        private FormationRoleType DetermineCavalryRole(Formation formation, TacticalStrategy strategy, BattlefieldAssessment assessment)
        {
            switch (strategy)
            {
                case TacticalStrategy.Aggressive:
                    return FormationRoleType.Charge;
                    
                case TacticalStrategy.Defensive:
                    return FormationRoleType.ProtectFlank;
                    
                case TacticalStrategy.Flanking:
                    return FormationRoleType.Flank;
                    
                case TacticalStrategy.Skirmish:
                    return FormationRoleType.HarassAndRetreat;
                    
                case TacticalStrategy.Retreat:
                    return FormationRoleType.DelayingAction;
                    
                case TacticalStrategy.Balanced:
                default:
                    // More situational decision for balanced strategy
                    if (assessment.EnemyArcherRatio > 0.3f)
                    {
                        return FormationRoleType.Flank;
                    }
                    else if (assessment.EnemyCavalryRatio > 0.3f)
                    {
                        return FormationRoleType.ProtectFlank;
                    }
                    else
                    {
                        return FormationRoleType.Reserve;
                    }
            }
        }
        
        /// <summary>
        /// Determine the role for a horse archer formation
        /// </summary>
        private FormationRoleType DetermineHorseArcherRole(Formation formation, TacticalStrategy strategy, BattlefieldAssessment assessment)
        {
            // Horse archers generally want to harass and maintain distance
            switch (strategy)
            {
                case TacticalStrategy.Aggressive:
                    return FormationRoleType.HarassAndRetreat;
                    
                case TacticalStrategy.Defensive:
                    return FormationRoleType.FireAtWill;
                    
                case TacticalStrategy.Flanking:
                    return FormationRoleType.Flank;
                    
                case TacticalStrategy.Skirmish:
                    return FormationRoleType.HarassAndRetreat;
                    
                case TacticalStrategy.Retreat:
                    return FormationRoleType.CoveringFire;
                    
                case TacticalStrategy.Balanced:
                default:
                    return FormationRoleType.HarassAndRetreat;
            }
        }
        
        /// <summary>
        /// Create tactical actions for each formation
        /// </summary>
        private void CreateFormationActions(TacticalPlan plan, Team team, Team enemyTeam, BattlefieldAssessment assessment)
        {
            try
            {
                // Clear existing actions
                plan.FormationActions.Clear();
                
                // For each role in the plan
                foreach (FormationRole role in plan.FormationRoles)
                {
                    // Create context for formation controller
                    BattlefieldContext context = new BattlefieldContext();
                    context.AggressionLevel = assessment.AggressionLevel;
                    context.CurrentTerrain = assessment.TerrainType.ToString();
                    context.OnHighGround = IsOnHighGround(role.Formation, assessment);
                    context.HasInfantryAdvantage = assessment.HasInfantryAdvantage;
                    context.HasRangedAdvantage = assessment.HasArcherAdvantage;
                    context.HasCavalryAdvantage = assessment.HasCavalryAdvantage;
                    
                    // Get optimal formation type
                    string formationType = _formationController.GetOptimalFormationType(role.Formation, enemyTeam, context);
                    
                    // Create action based on role
                    FormationAction action = CreateActionForRole(role, formationType, team, enemyTeam, assessment);
                    
                    // Add to plan
                    plan.FormationActions.Add(action);
                    
                    // Store pending action by formation class for combined arms coordination
                    _pendingActions[role.Formation.FormationIndex].Add(action);
                }
                
                // Apply combined arms coordination (adjust actions based on other formations)
                CoordinateFormationActions(plan, team, assessment);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error creating formation actions: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Create a specific tactical action for a formation role
        /// </summary>
        private FormationAction CreateActionForRole(
            FormationRole role, 
            string formationType, 
            Team team, 
            Team enemyTeam, 
            BattlefieldAssessment assessment)
        {
            FormationAction action = new FormationAction();
            action.Formation = role.Formation;
            action.FormationType = formationType;
            
            try
            {
                // Base action on the role
                switch (role.Role)
                {
                    case FormationRoleType.Assault:
                        action.ActionType = FormationActionType.Advance;
                        action.TargetPosition = GetEnemyCenterPosition(enemyTeam);
                        action.Priority = 1;
                        break;
                        
                    case FormationRoleType.HoldPosition:
                        action.ActionType = FormationActionType.Hold;
                        action.TargetPosition = GetFormationPosition(role.Formation);
                        action.Priority = 1;
                        break;
                        
                    case FormationRoleType.HoldCenter:
                        action.ActionType = FormationActionType.Hold;
                        action.TargetPosition = GetTeamCenterPosition(team);
                        action.Priority = 2;
                        break;
                        
                    case FormationRoleType.ScreenAdvance:
                        action.ActionType = FormationActionType.Advance;
                        action.TargetPosition = GetPositionInFrontOfTeam(team, 30f);
                        action.Priority = 2;
                        break;
                        
                    case FormationRoleType.FireSupport:
                        action.ActionType = FormationActionType.FireAt;
                        action.TargetPosition = GetEnemyCenterPosition(enemyTeam);
                        action.Priority = 2;
                        break;
                        
                    case FormationRoleType.FireAtWill:
                        action.ActionType = FormationActionType.FireAt;
                        action.TargetPosition = GetClosestEnemyPosition(role.Formation, enemyTeam);
                        action.Priority = 3;
                        break;
                        
                    case FormationRoleType.FireFromElevation:
                        action.ActionType = FormationActionType.FireAt;
                        action.TargetPosition = GetElevatedPosition(assessment);
                        action.Priority = 1;
                        break;
                        
                    case FormationRoleType.Flank:
                        action.ActionType = FormationActionType.Flank;
                        action.TargetPosition = GetFlankingPosition(role.Formation, enemyTeam);
                        action.Priority = 1;
                        break;
                        
                    case FormationRoleType.Charge:
                        action.ActionType = FormationActionType.Charge;
                        action.TargetPosition = GetBestChargeTarget(role.Formation, enemyTeam);
                        action.Priority = 1;
                        break;
                        
                    case FormationRoleType.HarassAndRetreat:
                        action.ActionType = FormationActionType.Harass;
                        action.TargetPosition = GetHarassmentPosition(role.Formation, enemyTeam);
                        action.Priority = 2;
                        break;
                        
                    case FormationRoleType.CoveringFire:
                        action.ActionType = FormationActionType.FireAt;
                        action.TargetPosition = GetPositionBetweenTeams(team, enemyTeam);
                        action.Priority = 3;
                        break;
                        
                    case FormationRoleType.ProtectFlank:
                        action.ActionType = FormationActionType.Guard;
                        action.TargetPosition = GetFlankProtectionPosition(team);
                        action.Priority = 2;
                        break;
                        
                    case FormationRoleType.RearGuard:
                        action.ActionType = FormationActionType.Guard;
                        action.TargetPosition = GetRearPosition(team);
                        action.Priority = 3;
                        break;
                        
                    case FormationRoleType.Reserve:
                        action.ActionType = FormationActionType.Hold;
                        action.TargetPosition = GetReservePosition(team);
                        action.Priority = 4;
                        break;
                        
                    case FormationRoleType.DelayingAction:
                        action.ActionType = FormationActionType.Harass;
                        action.TargetPosition = GetPositionBetweenTeams(team, enemyTeam);
                        action.Priority = 3;
                        break;
                        
                    default:
                        action.ActionType = FormationActionType.Hold;
                        action.TargetPosition = GetFormationPosition(role.Formation);
                        action.Priority = 5;
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error creating action for role {role.Role}: {ex.Message}");
                
                // Default fallback action
                action.ActionType = FormationActionType.Hold;
                action.TargetPosition = GetFormationPosition(role.Formation);
                action.Priority = 5;
            }
            
            return action;
        }
        
        /// <summary>
        /// Coordinate actions between different formations for combined arms tactics
        /// </summary>
        private void CoordinateFormationActions(TacticalPlan plan, Team team, BattlefieldAssessment assessment)
        {
            try
            {
                // Only coordinate if we have multiple formation types
                int formationTypeCount = 0;
                if (assessment.OurInfantryCount > 0) formationTypeCount++;
                if (assessment.OurArcherCount > 0) formationTypeCount++;
                if (assessment.OurCavalryCount > 0) formationTypeCount++;
                if (assessment.OurHorseArcherCount > 0) formationTypeCount++;
                
                if (formationTypeCount < 2) return;
                
                // Special case: Coordinate infantry advance with archer fire
                bool hasAssaultingInfantry = false;
                bool hasArchers = false;
                
                foreach (FormationAction action in plan.FormationActions)
                {
                    if (action.Formation.FormationIndex == FormationClass.Infantry && 
                        action.ActionType == FormationActionType.Advance)
                    {
                        hasAssaultingInfantry = true;
                    }
                    
                    if (action.Formation.FormationIndex == FormationClass.Ranged)
                    {
                        hasArchers = true;
                    }
                }
                
                // If we have both infantry advancing and archers, coordinate them
                if (hasAssaultingInfantry && hasArchers)
                {
                    CoordinateInfantryAdvanceWithArcherFire(plan);
                }
                
                // Special case: Coordinate cavalry flanking with infantry engagement
                bool hasInfantry = assessment.OurInfantryCount > 0;
                bool hasCavalry = assessment.OurCavalryCount > 0;
                
                if (hasInfantry && hasCavalry)
                {
                    CoordinateCavalryWithInfantry(plan);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error coordinating formation actions: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Coordinate infantry advance with archer fire support
        /// </summary>
        private void CoordinateInfantryAdvanceWithArcherFire(TacticalPlan plan)
        {
            try
            {
                // Get archer actions
                List<FormationAction> archerActions = new List<FormationAction>();
                
                foreach (FormationAction action in plan.FormationActions)
                {
                    if (action.Formation.FormationIndex == FormationClass.Ranged)
                    {
                        archerActions.Add(action);
                    }
                }
                
                // Ensure archers are firing at the same target infantry is advancing toward
                foreach (FormationAction infantryAction in plan.FormationActions)
                {
                    if (infantryAction.Formation.FormationIndex == FormationClass.Infantry && 
                        infantryAction.ActionType == FormationActionType.Advance)
                    {
                        foreach (FormationAction archerAction in archerActions)
                        {
                            // Set archers to fire at the same target
                            archerAction.ActionType = FormationActionType.FireAt;
                            archerAction.TargetPosition = infantryAction.TargetPosition;
                            archerAction.Priority = 1; // High priority
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error coordinating infantry and archers: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Coordinate cavalry actions with infantry engagement
        /// </summary>
        private void CoordinateCavalryWithInfantry(TacticalPlan plan)
        {
            try
            {
                // Get infantry and cavalry actions
                List<FormationAction> infantryActions = new List<FormationAction>();
                List<FormationAction> cavalryActions = new List<FormationAction>();
                
                foreach (FormationAction action in plan.FormationActions)
                {
                    if (action.Formation.FormationIndex == FormationClass.Infantry)
                    {
                        infantryActions.Add(action);
                    }
                    else if (action.Formation.FormationIndex == FormationClass.Cavalry)
                    {
                        cavalryActions.Add(action);
                    }
                }
                
                // If infantry is engaging, set cavalry to flank
                bool infantryEngaging = false;
                
                foreach (FormationAction infantryAction in infantryActions)
                {
                    if (infantryAction.ActionType == FormationActionType.Advance || 
                        infantryAction.ActionType == FormationActionType.Charge)
                    {
                        infantryEngaging = true;
                        break;
                    }
                }
                
                if (infantryEngaging)
                {
                    // Set cavalry to flank when infantry engages
                    foreach (FormationAction cavalryAction in cavalryActions)
                    {
                        if (cavalryAction.ActionType != FormationActionType.Charge)
                        {
                            cavalryAction.ActionType = FormationActionType.Flank;
                            cavalryAction.Priority = 1; // High priority
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Error coordinating cavalry with infantry: {ex.Message}");
            }
        }
        
        #region Position Helper Methods
        
        /// <summary>
        /// Get current position of a formation
        /// </summary>
        private Vec3 GetFormationPosition(Formation formation)
        {
            try
            {
                if (formation == null || formation.QuerySystem == null)
                {
                    return Vec3.Zero;
                }
                
                WorldPosition position = formation.QuerySystem.MedianPosition;
                if (position.GetNavMesh() != null)
                {
                    return position.GetGroundVec3();
                }
                return Vec3.Zero;
            }
            catch
            {
                return Vec3.Zero;
            }
        }
        
        /// <summary>
        /// Get center position of a team
        /// </summary>
        private Vec3 GetTeamCenterPosition(Team team)
        {
            try
            {
                if (team == null)
                {
                    return Vec3.Zero;
                }
                
                Vec3 totalPos = Vec3.Zero;
                int count = 0;
                
                foreach (Formation formation in team.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits <= 0) continue;
                    
                    Vec3 pos = GetFormationPosition(formation);
                    if (pos != Vec3.Zero)
                    {
                        totalPos += pos;
                        count++;
                    }
                }
                
                return count > 0 ? totalPos / count : Vec3.Zero;
            }
            catch
            {
                return Vec3.Zero;
            }
        }
        
        /// <summary>
        /// Get center position of enemy team
        /// </summary>
        private Vec3 GetEnemyCenterPosition(Team enemyTeam)
        {
            return GetTeamCenterPosition(enemyTeam);
        }
        
        /// <summary>
        /// Get a position in front of the team in their facing direction
        /// </summary>
        private Vec3 GetPositionInFrontOfTeam(Team team, float distance)
        {
            try
            {
                Vec3 teamCenter = GetTeamCenterPosition(team);
                
                // Calculate team direction
                Vec3 teamDir = Vec3.Zero;
                int count = 0;
                
                foreach (Formation formation in team.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits <= 0) continue;
                    
                    teamDir += new Vec3(formation.Direction.X, formation.Direction.Y, 0);
                    count++;
                }
                
                if (count > 0)
                {
                    teamDir /= count;
                    teamDir.Normalize();
                    
                    // Position in front of team
                    return teamCenter + (teamDir * distance);
                }
                
                return teamCenter;
            }
            catch
            {
                return GetTeamCenterPosition(team);
            }
        }
        
        /// <summary>
        /// Get position between two teams
        /// </summary>
        private Vec3 GetPositionBetweenTeams(Team team1, Team team2)
        {
            try
            {
                Vec3 pos1 = GetTeamCenterPosition(team1);
                Vec3 pos2 = GetTeamCenterPosition(team2);
                
                return (pos1 + pos2) * 0.5f;
            }
            catch
            {
                return Vec3.Zero;
            }
        }
        
        /// <summary>
        /// Get position for flanking maneuver
        /// </summary>
        private Vec3 GetFlankingPosition(Formation formation, Team enemyTeam)
        {
            try
            {
                Vec3 enemyCenter = GetTeamCenterPosition(enemyTeam);
                Vec3 formationPos = GetFormationPosition(formation);
                
                // Calculate enemy team direction
                Vec3 enemyDir = Vec3.Zero;
                int count = 0;
                
                foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (enemyFormation.CountOfUnits <= 0) continue;
                    
                    enemyDir += new Vec3(enemyFormation.Direction.X, enemyFormation.Direction.Y, 0);
                    count++;
                }
                
                if (count > 0)
                {
                    enemyDir /= count;
                    enemyDir.Normalize();
                    
                    // Get perpendicular vector (to the right)
                    Vec3 rightFlank = new Vec3(enemyDir.y, -enemyDir.x, 0);
                    rightFlank.Normalize();
                    
                    // Choose flank direction
                    Vec3 flankDir;
                    
                    // Determine which flank is more open
                    bool rightFlankClear = IsFlankClear(enemyCenter, rightFlank, enemyTeam);
                    bool leftFlankClear = IsFlankClear(enemyCenter, -rightFlank, enemyTeam);
                    
                    if (rightFlankClear && !leftFlankClear)
                    {
                        flankDir = rightFlank;
                    }
                    else if (leftFlankClear && !rightFlankClear)
                    {
                        flankDir = -rightFlank;
                    }
                    else
                    {
                        // Both or neither flanks clear, choose based on formation position
                        Vec3 toFormation = formationPos - enemyCenter;
                        
                        flankDir = (Vec3.DotProduct(toFormation, rightFlank) > 0) ? rightFlank : -rightFlank;
                    }
                    
                    // Position on the flank
                    return enemyCenter + (flankDir * 60f);
                }
                
                // Fallback to a position to the right of enemy
                return enemyCenter + new Vec3(60f, 0, 0);
            }
            catch
            {
                return GetTeamCenterPosition(enemyTeam);
            }
        }
        
        /// <summary>
        /// Check if a flank direction is clear of enemy units
        /// </summary>
        private bool IsFlankClear(Vec3 startPos, Vec3 direction, Team enemyTeam)
        {
            try
            {
                Vec3 checkPos = startPos + (direction * 40f);
                
                foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (enemyFormation.CountOfUnits <= 0) continue;
                    
                    Vec3 formationPos = GetFormationPosition(enemyFormation);
                    
                    // Check distance to this potential checkpoint
                    float distSq = (checkPos - formationPos).LengthSquared;
                    
                    if (distSq < 30 * 30)
                    {
                        return false; // Enemy formation too close to flank route
                    }
                }
                
                return true;
            }
            catch
            {
                return true;
            }
        }
        
        /// <summary>
        /// Get best target for a cavalry charge
        /// </summary>
        private Vec3 GetBestChargeTarget(Formation formation, Team enemyTeam)
        {
            try
            {
                // Look for ranged units first, then infantry
                Formation bestTarget = null;
                float bestScore = 0;
                
                foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (enemyFormation.CountOfUnits <= 0) continue;
                    
                    float score = 0;
                    
                    // Prioritize archers
                    if (enemyFormation.FormationIndex == FormationClass.Ranged)
                    {
                        score = 100 + enemyFormation.CountOfUnits;
                    }
                    // Then horse archers
                    else if (enemyFormation.FormationIndex == FormationClass.HorseArcher)
                    {
                        score = 80 + enemyFormation.CountOfUnits;
                    }
                    // Then infantry if not in square/spear formation
                    else if (enemyFormation.FormationIndex == FormationClass.Infantry)
                    {
                        if (enemyFormation.ArrangementOrder == null ||
                            !enemyFormation.ArrangementOrder.OrderType.ToString().Contains("Square"))
                        {
                            score = 50 + enemyFormation.CountOfUnits;
                        }
                    }
                    
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = enemyFormation;
                    }
                }
                
                if (bestTarget != null)
                {
                    return GetFormationPosition(bestTarget);
                }
                
                // Fallback to enemy center
                return GetEnemyCenterPosition(enemyTeam);
            }
            catch
            {
                return GetEnemyCenterPosition(enemyTeam);
            }
        }
        
        /// <summary>
        /// Get position for harassment actions
        /// </summary>
        private Vec3 GetHarassmentPosition(Formation formation, Team enemyTeam)
        {
            try
            {
                // For harassment, get a position that's at range but still close enough to fire
                Vec3 formationPos = GetFormationPosition(formation);
                Vec3 enemyCenter = GetEnemyCenterPosition(enemyTeam);
                
                // Get direction from enemy to formation
                Vec3 direction = formationPos - enemyCenter;
                
                // If close to zero, pick a random direction
                if (direction.LengthSquared < 0.1f)
                {
                    direction = new Vec3((float)new Random().NextDouble(), (float)new Random().NextDouble(), 0);
                }
                
                direction.Normalize();
                
                // Position at skirmishing range from enemy
                float range = (formation.FormationIndex == FormationClass.Ranged) ? 70f : 50f;
                
                return enemyCenter + (direction * range);
            }
            catch
            {
                return GetFormationPosition(formation);
            }
        }
        
        /// <summary>
        /// Get position to guard a team's flank
        /// </summary>
        private Vec3 GetFlankProtectionPosition(Team team)
        {
            try
            {
                Vec3 teamCenter = GetTeamCenterPosition(team);
                
                // Calculate team direction
                Vec3 teamDir = Vec3.Zero;
                int count = 0;
                
                foreach (Formation formation in team.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits <= 0) continue;
                    
                    teamDir += new Vec3(formation.Direction.X, formation.Direction.Y, 0);
                    count++;
                }
                
                if (count > 0)
                {
                    teamDir /= count;
                    teamDir.Normalize();
                    
                    // Get perpendicular vector (to the right)
                    Vec3 rightFlank = new Vec3(teamDir.y, -teamDir.x, 0);
                    rightFlank.Normalize();
                    
                    // Alternate between left and right flank
                    bool chooseRightFlank = DateTime.Now.Second % 2 == 0;
                    
                    // Position on the flank
                    return teamCenter + ((chooseRightFlank ? rightFlank : -rightFlank) * 40f);
                }
                
                // Fallback
                return teamCenter + new Vec3(40f, 0, 0);
            }
            catch
            {
                return GetTeamCenterPosition(team);
            }
        }
        
        /// <summary>
        /// Get position to the rear of a team
        /// </summary>
        private Vec3 GetRearPosition(Team team)
        {
            try
            {
                Vec3 teamCenter = GetTeamCenterPosition(team);
                
                // Calculate team direction
                Vec3 teamDir = Vec3.Zero;
                int count = 0;
                
                foreach (Formation formation in team.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits <= 0) continue;
                    
                    teamDir += new Vec3(formation.Direction.X, formation.Direction.Y, 0);
                    count++;
                }
                
                if (count > 0)
                {
                    teamDir /= count;
                    teamDir.Normalize();
                    
                    // Position to the rear
                    return teamCenter - (teamDir * 40f);
                }
                
                // Fallback
                return teamCenter - new Vec3(40f, 0, 0);
            }
            catch
            {
                return GetTeamCenterPosition(team);
            }
        }
        
        /// <summary>
        /// Get position for reserve forces
        /// </summary>
        private Vec3 GetReservePosition(Team team)
        {
            try
            {
                // Reserve position is similar to rear, but not as far back
                Vec3 teamCenter = GetTeamCenterPosition(team);
                
                // Calculate team direction
                Vec3 teamDir = Vec3.Zero;
                int count = 0;
                
                foreach (Formation formation in team.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits <= 0) continue;
                    
                    teamDir += new Vec3(formation.Direction.X, formation.Direction.Y, 0);
                    count++;
                }
                
                if (count > 0)
                {
                    teamDir /= count;
                    teamDir.Normalize();
                    
                    // Position slightly to the rear and side
                    Vec3 rightFlank = new Vec3(teamDir.y, -teamDir.x, 0);
                    rightFlank.Normalize();
                    
                    return teamCenter - (teamDir * 20f) + (rightFlank * 20f);
                }
                
                // Fallback
                return teamCenter - new Vec3(20f, 20f, 0);
            }
            catch
            {
                return GetTeamCenterPosition(team);
            }
        }
        
        /// <summary>
        /// Get elevated position from terrain analysis
        /// </summary>
        private Vec3 GetElevatedPosition(BattlefieldAssessment assessment)
        {
            try
            {
                if (assessment.HighGroundPositions.Count > 0)
                {
                    return assessment.HighGroundPositions[0]; // Vec3 is already a position
                }
                
                // Fallback
                return Vec3.Zero;
            }
            catch
            {
                return Vec3.Zero;
            }
        }
        
        /// <summary>
        /// Get closest enemy position to a formation
        /// </summary>
        private Vec3 GetClosestEnemyPosition(Formation formation, Team enemyTeam)
        {
            try
            {
                Vec3 formationPos = GetFormationPosition(formation);
                Vec3 closestPos = GetEnemyCenterPosition(enemyTeam);
                float closestDistSq = float.MaxValue;
                
                foreach (Formation enemyFormation in enemyTeam.FormationsIncludingEmpty)
                {
                    if (enemyFormation.CountOfUnits <= 0) continue;
                    
                    Vec3 enemyPos = GetFormationPosition(enemyFormation);
                    float distSq = (formationPos - enemyPos).LengthSquared;
                    
                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closestPos = enemyPos;
                    }
                }
                
                return closestPos;
            }
            catch
            {
                return GetEnemyCenterPosition(enemyTeam);
            }
        }
        
        /// <summary>
        /// Check if a formation is on high ground
        /// </summary>
        private bool IsOnHighGround(Formation formation, BattlefieldAssessment assessment)
        {
            try
            {
                if (assessment.HighGroundPositions.Count == 0)
                {
                    return false;
                }
                
                Vec3 formationPos = GetFormationPosition(formation);
                
                // In our simplified version, HighGroundPositions are just Vec3 positions
                foreach (Vec3 highGroundPos in assessment.HighGroundPositions)
                {
                    float distSq = (formationPos - highGroundPos).LengthSquared;
                    
                    // Using a fixed radius for our simplified implementation
                    if (distSq < 100.0f * 100.0f) // 100 meter radius
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
        
        /// <summary>
        /// Check if player is in a team
        /// </summary>
        private bool IsPlayerInTeam(Team team)
        {
            if (team == null)
            {
                return false;
            }
            
            try
            {
                // Check if player's agent is on this team
                Agent playerAgent = Agent.Main;
                
                if (playerAgent != null && playerAgent.Team == team)
                {
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Reset for a new battle
        /// </summary>
        public void Reset()
        {
            _teamPlans.Clear();
            _battlefieldAssessments.Clear();
            _lastPlanningTime = DateTime.Now;
            
            foreach (FormationClass formationClass in _pendingActions.Keys)
            {
                _pendingActions[formationClass].Clear();
            }
            
            _formationController.Reset();
            // Reset terrain analyzer
            Logger.Instance.Info("Resetting terrain analyzer");
            
            Logger.Instance.Info("TacticalPlanner reset for new battle");
        }
    }
    
    /// <summary>
    /// Overall tactical plan for a team
    /// </summary>
    public class TacticalPlan
    {
        public TacticalStrategy Strategy { get; set; } = TacticalStrategy.Balanced;
        public List<FormationRole> FormationRoles { get; set; } = new List<FormationRole>();
        public List<FormationAction> FormationActions { get; set; } = new List<FormationAction>();
        public DateTime CreationTime { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// Assessment of the battlefield situation
    /// </summary>
    public class BattlefieldAssessment
    {
        // Overall force composition
        public int OurInfantryCount { get; set; }
        public int OurArcherCount { get; set; }
        public int OurCavalryCount { get; set; }
        public int OurHorseArcherCount { get; set; }
        public int OurTotalTroops { get; set; }
        
        public int EnemyInfantryCount { get; set; }
        public int EnemyArcherCount { get; set; }
        public int EnemyCavalryCount { get; set; }
        public int EnemyHorseArcherCount { get; set; }
        public int EnemyTotalTroops { get; set; }
        
        // Force composition ratios
        public float OurInfantryRatio { get; set; }
        public float OurArcherRatio { get; set; }
        public float OurCavalryRatio { get; set; }
        public float OurHorseArcherRatio { get; set; }
        
        public float EnemyInfantryRatio { get; set; }
        public float EnemyArcherRatio { get; set; }
        public float EnemyCavalryRatio { get; set; }
        public float EnemyHorseArcherRatio { get; set; }
        
        // Tactical advantages
        public bool HasInfantryAdvantage { get; set; }
        public bool HasArcherAdvantage { get; set; }
        public bool HasCavalryAdvantage { get; set; }
        public bool HasHorseArcherAdvantage { get; set; }
        public bool HasCompositionAdvantage { get; set; }
        
        // Terrain situation
        public HannibalAI.Terrain.TerrainType TerrainType { get; set; }
        public bool HasHighGround { get; set; }
        public bool HasChokepoints { get; set; }
        public bool HasForestCover { get; set; }
        public List<Vec3> HighGroundPositions { get; set; } = new List<Vec3>();
        public List<Vec3> ChokepointPositions { get; set; } = new List<Vec3>();
        
        // Overall assessment
        public float StrengthRatio { get; set; } = 1.0f;
        public bool HasOverallAdvantage { get; set; }
        public bool HasOverallDisadvantage { get; set; }
        
        // Commander influence
        public float AggressionLevel { get; set; } = 0.5f;
        public bool HasVendettaAgainstPlayer { get; set; }
        
        // Player relationship
        public bool IsPlayerTeam { get; set; }
        public bool IsAgainstPlayer { get; set; }
    }
    
    /// <summary>
    /// Role assigned to a formation
    /// </summary>
    public class FormationRole
    {
        public Formation Formation { get; set; }
        public FormationRoleType Role { get; set; }
    }
    
    /// <summary>
    /// Action to be taken by a formation
    /// </summary>
    public class FormationAction
    {
        public Formation Formation { get; set; }
        public FormationActionType ActionType { get; set; }
        public Vec3 TargetPosition { get; set; }
        public string FormationType { get; set; }
        public int Priority { get; set; } = 5; // 1 is highest, 5 is lowest
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Types of overall tactical strategies
    /// </summary>
    public enum TacticalStrategy
    {
        Aggressive,  // Focus on attacking
        Defensive,   // Focus on holding position
        Flanking,    // Focus on flanking maneuvers
        Skirmish,    // Focus on harassment and ranged attacks
        Retreat,     // Focus on fallback and defensive withdrawal
        Balanced     // Balanced approach
    }
    
    /// <summary>
    /// Types of roles formations can be assigned
    /// </summary>
    public enum FormationRoleType
    {
        Assault,          // Aggressively attack enemy
        HoldPosition,     // Hold current position
        HoldCenter,       // Hold center of formation
        ScreenAdvance,    // Advance as a screening force
        FireSupport,      // Provide fire support
        FireAtWill,       // Fire at nearest targets
        FireFromElevation, // Fire from elevated position
        Flank,            // Execute flanking maneuver
        Charge,           // Execute direct charge
        HarassAndRetreat, // Harass enemy then retreat
        CoveringFire,     // Provide covering fire
        ProtectFlank,     // Protect team's flank
        RearGuard,        // Guard the rear
        Reserve,          // Stay in reserve
        DelayingAction    // Delay enemy advance
    }
    
    /// <summary>
    /// Types of actions formations can take
    /// </summary>
    public enum FormationActionType
    {
        Hold,             // Hold position
        Advance,          // Advance toward position
        Charge,           // Charge toward position
        Retreat,          // Retreat to position
        FireAt,           // Fire at position
        Flank,            // Flank toward position
        Harass,           // Harass enemy at position
        Guard             // Guard position
    }
}