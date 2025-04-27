using System;
using HannibalAI.Battle;
using HannibalAI.Command;
using HannibalAI.Utils;

namespace HannibalAI.Services
{
    public class AIService
    {
        private readonly BattleController _battleController;

        public AIService(BattleController battleController)
        {
            _battleController = battleController ?? throw new ArgumentNullException(nameof(battleController));
        }

        public void ProcessBattleSnapshot(BattleSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            try
            {
                // Example decision-making logic based on the battle snapshot
                var decision = new AIDecision
                {
                    Command = new MoveFormationCommand
                    {
                        Formation = 0, // First friendly formation as a placeholder
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
