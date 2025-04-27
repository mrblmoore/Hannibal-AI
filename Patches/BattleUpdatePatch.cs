using HarmonyLib;
using TaleWorlds.MountAndBlade;
using HannibalAI.Battle;
using HannibalAI.Services;
using HannibalAI.Utils;

namespace HannibalAI.Patches
{
    [HarmonyPatch(typeof(Mission), "OnMissionTick")]
    public class BattleUpdatePatch
    {
        private static BattleController _battleController;
        private static AIService _aiService;
        private static bool _battleStarted;
        private static string _currentCommanderId;

        static void Postfix(Mission __instance)
        {
            if (__instance == null || __instance.PlayerTeam == null)
                return;

            if (!_battleStarted)
            {
                _battleStarted = true;
                _currentCommanderId = System.Guid.NewGuid().ToString();

                Logger.LogInfo($"Battle started. Commander ID: {_currentCommanderId}");

                var fallbackService = new FallbackService();
                _battleController = new BattleController(new AICommander(), fallbackService);
                _aiService = new AIService(fallbackService);
            }

            try
            {
                var snapshot = new BattleSnapshot(__instance);

                if (snapshot != null && _aiService != null)
                {
                    var decision = _aiService.GetDecision(snapshot);

                    if (decision != null && _battleController != null)
                    {
                        _battleController.ExecuteAIDecision(decision);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"BattleUpdatePatch exception: {ex.Message}");
            }
        }
    }
}
