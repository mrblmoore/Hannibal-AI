using HarmonyLib;
using TaleWorlds.MountAndBlade;
using Hannibal;
using Hannibal.AI;
using Hannibal.Configuration;

namespace Hannibal
{
    [HarmonyPatch(typeof(Mission), "Tick")]
    public class BattleUpdatePatch
    {
        private static BattleController _battleController;

        static void Postfix(Mission __instance, float dt)
        {
            if (__instance != null && __instance.Mode == MissionMode.Battle)
            {
                if (_battleController == null)
                {
                    _battleController = new BattleController(__instance);
                }

                try
                {
                    var commander = new AICommander(__instance.PlayerTeam);
                    var decision = commander.GenerateAIDecision();

                    if (decision != null)
                    {
                        _battleController.ExecuteAIDecision(decision);

                        // Update commander memory
                        if (ModConfig.Instance.UseCommanderMemory)
                        {
                            var memoryService = new CommanderMemoryService();
                            memoryService.RecordCommanderInteraction(commander.GetCommanderId());
                            memoryService.DecayMemoryOverTime();
                        }

                        if (ModConfig.Instance.Debug)
                        {
                            Logger.Instance.Info($"Executed AI decision: {decision.Command.GetType().Name}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.Error($"Error during AI decision execution: {e}");
                }
            }
        }
    }
}