using System;
using TaleWorlds.MountAndBlade;
using HannibalAI.Battle;
using HannibalAI.Command;

namespace HannibalAI.Services
{
    public class FallbackService
    {
        private readonly Mission _mission;

        public FallbackService(Mission mission)
        {
            _mission = mission ?? throw new ArgumentNullException(nameof(mission));
        }

        public AICommand GetFallbackDecision()
        {
            if (_mission == null)
                return null;

            var playerTeam = _mission.PlayerTeam;
            if (playerTeam == null)
                return null;

            // Example fallback: move all formations to a defensive line
            foreach (var formation in playerTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    var fallbackPosition = formation.GetMedianPosition();
                    return new HoldCommand
                    {
                        Formation = formation,
                        HoldPosition = fallbackPosition
                    };
                }
            }

            return null;
        }
    }
}
