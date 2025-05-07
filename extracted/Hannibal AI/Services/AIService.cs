using HannibalAI.Battle;
using HannibalAI.Command;
using HannibalAI.Utils;
using System;

namespace HannibalAI.Services
{
    public class AIService
    {
        private readonly BattleController _battleController;

        public AIService(BattleController battleController)
        {
            _battleController = battleController;
        }

        /// <summary>
        /// [Future Feature]
        /// Process a real-time battle snapshot and generate an AI decision dynamically.
        /// Currently unused — will be connected to battlefield monitoring later.
        /// </summary>
        public void ProcessBattleSnapshot(BattleSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            try
            {
                var decision = new AIDecision
                {
                    Command = new MoveFormationCommand
                    {
                        Formation = 0, // Placeholder formation
                        TargetPosition = snapshot.FriendlyFormations?[0].Position ?? default
                    }
                };

                _battleController.ExecuteAIDecision(decision);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error processing battle snapshot: {ex.Message}");
            }
        }
    }
}
