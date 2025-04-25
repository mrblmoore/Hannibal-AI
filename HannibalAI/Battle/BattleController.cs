using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.Engine;

namespace HannibalAI.Battle
{
    public class BattleController
    {
        private Mission _mission;
        private BattleSnapshot _currentSnapshot;
        private List<BattleSnapshot> _battleHistory;

        public BattleController(Mission mission)
        {
            _mission = mission;
            _battleHistory = new List<BattleSnapshot>();
        }

        public void Update(float dt)
        {
            _currentSnapshot = CreateSnapshot();
            _battleHistory.Add(_currentSnapshot);
        }

        private BattleSnapshot CreateSnapshot()
        {
            var snapshot = new BattleSnapshot
            {
                Time = _mission.CurrentTime,
                Scene = _mission.Scene,
                MapSize = _mission.Scene.GetBoundingBoxSize()
            };

            foreach (var agent in _mission.Agents)
            {
                var unitSnapshot = new UnitSnapshot
                {
                    UnitId = agent.Index,
                    Position = agent.Position,
                    Direction = agent.Direction,
                    Health = agent.Health,
                    FormationIndex = agent.Formation?.Index ?? -1,
                    IsPlayerControlled = agent.IsPlayerControlled
                };
                snapshot.Units.Add(unitSnapshot);
            }

            return snapshot;
        }

        public BattleSnapshot GetCurrentSnapshot()
        {
            return _currentSnapshot;
        }

        public List<BattleSnapshot> GetBattleHistory()
        {
            return _battleHistory;
        }
    }
} 