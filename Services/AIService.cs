using HannibalAI.Battle;
using HannibalAI.Command;
using HannibalAI.Config;
using HannibalAI.Utils;
using TaleWorlds.MountAndBlade;
using System;
using System.Threading.Tasks;

namespace HannibalAI.Services
{
    public class AIService
    {
        private readonly ModConfig _config;
        private readonly FallbackService _fallbackService;
        private readonly BattleController _battleController;

        public AIService(BattleController battleController)
        {
            _config = ModConfig.Instance;
            _fallbackService = new FallbackService();
            _battleController = battleController ?? throw new ArgumentNullException(nameof(battleController));
        }

        public async Task ProcessBattleSnapshot(BattleSnapshot snapshot)
        {
            if (snapshot == null)
            {
                Logger.LogError("Null snapshot passed to AIService.ProcessBattleSnapshot.");
                return;
            }

            try
            {
                AIDecision decision = await GenerateDecisionAsync(snapshot);

                if (decision != null)
                {
                    _battleController.ExecuteAIDecision(decision);
                }
                else
                {
                    if (_config.Debug)
                        Logger.LogInfo("No valid AI decision generated. Falling back.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in ProcessBattleSnapshot: {ex.Message}");
            }
        }

        private async Task<AIDecision> GenerateDecisionAsync(BattleSnapshot snapshot)
        {
            try
            {
                // Future: Insert advanced decision-making logic here
                await Task.Yield(); // Placeholder to simulate async work

                // Simple fallback for now
                return _fallbackService.GetFallbackDecision(snapshot);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error generating AI decision: {ex.Message}");
                return null;
            }
        }
    }
}
