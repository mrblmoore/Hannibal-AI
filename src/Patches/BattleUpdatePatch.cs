using System;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using HannibalAI;

namespace HannibalAI.Patches
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
                    // Create AIService to pass to BattleController
                    var aiService = new AIService(ModConfig.Instance);
                    _battleController = new BattleController(aiService);
                }

                try
                {
                    // Simply let the BattleController handle the AI update during mission tick
                    // It has all the logic needed inside
                    _battleController.OnMissionTick(dt);
                    
                    if (ModConfig.Instance.Debug)
                    {
                        Logger.Instance.Info("Harmony patch triggered battle controller tick");
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.Error($"Error during battle controller tick: {e.Message}");
                }
            }
        }
    }
}