using System;
using System.Threading.Tasks;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Core;
using HannibalAI.Config;
using HannibalAI.Services;

namespace HannibalAI.Battle
{
    public class BattleController
    {
        private static BattleController _instance;
        private readonly ModConfig _config;

        private BattleController()
        {
            _config = ModConfig.Instance;
        }

        public static BattleController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BattleController();
                }
                return _instance;
            }
        }

        public async Task UpdateBattle(Mission mission, float dt)
        {
            try
            {
                if (!_config.Enabled || mission == null || !mission.IsFieldBattle)
                    return;

                var snapshot = BattleSnapshot.CreateFromMission(mission);
                if (snapshot == null)
                    return;

                var decision = await _aiService.ProcessBattleSnapshot(snapshot);
                if (decision == null)
                    return;

                ExecuteAIDecision(mission, decision);

                if (_config.Debug.ShowAIDecisions)
                {
                    Debug.Print($"[HannibalAI] Executed decision: {decision.Action} - {decision.Reasoning}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error in battle update: {ex.Message}");
            }
        }

        private void ExecuteAIDecision(Mission mission, AIDecision decision)
        {
            try
            {
                var team = mission.PlayerEnemyTeam;
                if (team == null || !team.HasTeamAi)
                    return;

                foreach (var command in decision.Commands)
                {
                    switch (command.Type.ToLower())
                    {
                        case "formation":
                            ExecuteFormationCommand(team.GetFormation(GetFormationClass(command.Parameters[0].ToString())), command.Type, command.Parameters);
                            break;
                        case "movement":
                            ExecuteMovementCommand(team.GetFormation(GetFormationClass(command.Parameters[0].ToString())), command.Type, command.Parameters);
                            break;
                        case "targeting":
                            ExecuteTargetingCommand(team.GetFormation(GetFormationClass(command.Parameters[0].ToString())), command.Type, command.Parameters);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error executing decision: {ex.Message}");
            }
        }

        public void ExecuteFormationCommand(Formation formation, string commandType, object[] parameters)
        {
            try
            {
                if (formation == null) return;

                switch (commandType.ToLower())
                {
                    case "line":
                        formation.FormOrderType = FormOrder.Line;
                        break;
                    case "shield_wall":
                        formation.FormOrderType = FormOrder.ShieldWall;
                        break;
                    case "loose":
                        formation.FormOrderType = FormOrder.Loose;
                        break;
                    case "circle":
                        formation.FormOrderType = FormOrder.Circle;
                        break;
                    case "square":
                        formation.FormOrderType = FormOrder.Square;
                        break;
                    case "column":
                        formation.FormOrderType = FormOrder.Column;
                        break;
                }

                formation.SetControlledByAI(true);
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error executing formation command: {ex.Message}");
            }
        }

        public void ExecuteMovementCommand(Formation formation, string commandType, object[] parameters)
        {
            try
            {
                if (formation == null) return;

                switch (commandType.ToLower())
                {
                    case "advance":
                        var distance = parameters?.Length > 0 ? Convert.ToSingle(parameters[0]) : 30f;
                        formation.SetMovementOrder(MovementOrder.MovementOrderAdvance);
                        formation.SetControlledByAI(true);
                        break;

                    case "charge":
                        formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                        formation.SetControlledByAI(true);
                        break;

                    case "retreat":
                        formation.SetMovementOrder(MovementOrder.MovementOrderRetreat);
                        formation.SetControlledByAI(true);
                        break;

                    case "hold":
                        formation.SetMovementOrder(MovementOrder.MovementOrderStop);
                        formation.SetControlledByAI(true);
                        break;

                    case "flank":
                        if (parameters?.Length > 1)
                        {
                            var direction = (Vec2)parameters[0];
                            var distance = Convert.ToSingle(parameters[1]);
                            var worldPosition = formation.QuerySystem.MedianPosition.ToWorldPosition();
                            var targetPosition = worldPosition.Advance2D(direction, distance);
                            formation.SetMovementOrder(MovementOrder.MovementOrderMove(targetPosition));
                            formation.SetControlledByAI(true);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error executing movement command: {ex.Message}");
            }
        }

        public void ExecuteTargetingCommand(Formation formation, string commandType, object[] parameters)
        {
            try
            {
                if (formation == null) return;

                switch (commandType.ToLower())
                {
                    case "focus_fire":
                        if (parameters?.Length > 0 && parameters[0] is Formation targetFormation)
                        {
                            formation.SetRangedAttackOrder(targetFormation);
                            formation.SetControlledByAI(true);
                        }
                        break;

                    case "hold_fire":
                        formation.SetRangedAttackOrder(null);
                        formation.SetControlledByAI(true);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[HannibalAI] Error executing targeting command: {ex.Message}");
            }
        }

        private FormationClass GetFormationClass(string formationName)
        {
            switch (formationName.ToLower())
            {
                case "infantry":
                case "main_force":
                    return FormationClass.Infantry;
                case "archers":
                case "ranged":
                    return FormationClass.Ranged;
                case "cavalry":
                    return FormationClass.Cavalry;
                case "horse_archers":
                    return FormationClass.HorseArcher;
                default:
                    return FormationClass.Infantry;
            }
        }

        private WorldPosition GetAdvancePosition(Formation formation, string style)
        {
            var currentPos = formation.CurrentPosition;
            var direction = formation.Direction;
            float distance = style.ToLower() switch
            {
                "aggressive" => 50f,
                "cautious" => 20f,
                _ => 35f
            };

            return currentPos.Advance(direction, distance);
        }

        private WorldPosition GetRetreatPosition(Formation formation)
        {
            var currentPos = formation.CurrentPosition;
            var direction = formation.Direction;
            return currentPos.Advance(direction, -30f);
        }

        private WorldPosition GetFlankingPosition(Formation formation, Team team)
        {
            var enemyCenter = team.EnemyTeam.AveragePosition;
            var currentPos = formation.CurrentPosition;
            var flankDirection = (enemyCenter - currentPos).RotateAboutZ(MathF.PI / 2);
            return currentPos.Advance(flankDirection, 40f);
        }

        private Formation GetTargetFormation(Team team, string targetType)
        {
            var enemyTeam = team.EnemyTeam;
            switch (targetType.ToLower())
            {
                case "enemy_infantry":
                    return enemyTeam.GetFormation(FormationClass.Infantry);
                case "enemy_archers":
                    return enemyTeam.GetFormation(FormationClass.Ranged);
                case "enemy_cavalry":
                    return enemyTeam.GetFormation(FormationClass.Cavalry);
                default:
                    return null;
            }
        }

        public Formation GetFormation(Team team, string formationType)
        {
            if (team == null) return null;

            switch (formationType.ToLower())
            {
                case "infantry":
                case "main_force":
                    return team.GetFormation(FormationClass.Infantry);
                case "archers":
                case "ranged":
                    return team.GetFormation(FormationClass.Ranged);
                case "cavalry":
                    return team.GetFormation(FormationClass.Cavalry);
                case "horse_archers":
                    return team.GetFormation(FormationClass.HorseArcher);
                default:
                    return null;
            }
        }
    }
} 