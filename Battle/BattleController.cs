using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using HannibalAI.Command;
using HannibalAI.Services;

namespace HannibalAI.Battle
{
    public class BattleController
    {
        private readonly AICommander _aiCommander;
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 1.0f;
        private bool _missionStarted = false;

        public BattleController()
        {
            _aiCommander = new AICommander();
        }

        public void Update(float dt)
        {
            if (!_missionStarted && Mission.Current != null)
            {
                _missionStarted = true;
                InitializeBattle();
            }

            if (_missionStarted && Mission.Current != null)
            {
                _lastUpdateTime += dt;
                if (_lastUpdateTime >= UPDATE_INTERVAL)
                {
                    UpdateBattleState();
                    _lastUpdateTime = 0f;
                }
            }
        }

        private void InitializeBattle()
        {
            try
            {
                // Initialize battle state
                var snapshot = BattleSnapshot.CreateFromMission(Mission.Current, "player");
                if (snapshot != null)
                {
                    var decision = _aiCommander.MakeDecision(snapshot);
                    if (decision != null)
                    {
                        ExecuteAIDecision(decision);
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"Error initializing battle: {ex.Message}");
            }
        }

        private void UpdateBattleState()
        {
            try
            {
                var snapshot = BattleSnapshot.CreateFromMission(Mission.Current, "player");
                if (snapshot != null)
                {
                    var decision = _aiCommander.MakeDecision(snapshot);
                    if (decision != null)
                    {
                        ExecuteAIDecision(decision);
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"Error updating battle state: {ex.Message}");
            }
        }

        private void ExecuteAIDecision(AIDecision decision)
        {
            if (decision?.Commands == null || decision.Commands.Length == 0)
            {
                return;
            }

            foreach (var command in decision.Commands)
            {
                try
                {
                    if (command == null) continue;

                    switch (command)
                    {
                        case MoveFormationCommand moveCommand:
                            ExecuteMoveFormation(moveCommand);
                            break;
                        case AttackFormationCommand attackCommand:
                            ExecuteAttackFormation(attackCommand);
                            break;
                        case ChangeFormationCommand changeCommand:
                            ExecuteChangeFormation(changeCommand);
                            break;
                        default:
                            TaleWorlds.Library.Debug.Print($"Unknown command type: {command.GetType().Name}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    TaleWorlds.Library.Debug.Print($"Error executing command: {ex.Message}");
                }
            }
        }

        private void ExecuteMoveFormation(MoveFormationCommand command)
        {
            var formation = GetFormation(command.FormationId);
            if (formation != null)
            {
                var targetPos = new Vec2(command.TargetPosition.X, command.TargetPosition.Y);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(targetPos));
            }
        }

        private void ExecuteAttackFormation(AttackFormationCommand command)
        {
            var formation = GetFormation(command.FormationId);
            var targetFormation = GetFormation(command.TargetFormationId);
            if (formation != null && targetFormation != null)
            {
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                formation.SetTargetFormation(targetFormation);
            }
        }

        private void ExecuteChangeFormation(ChangeFormationCommand command)
        {
            var formation = GetFormation(command.FormationId);
            if (formation != null)
            {
                formation.SetWidth(command.Width);
            }
        }

        private Formation GetFormation(int formationId)
        {
            if (Mission.Current == null) return null;

            foreach (var team in Mission.Current.Teams)
            {
                var formation = team.FormationsIncludingEmpty.FirstOrDefault(f => f.Index == formationId);
                if (formation != null)
                {
                    return formation;
                }
            }
            return null;
        }
    }
} 