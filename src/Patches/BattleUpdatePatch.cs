using System;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using HannibalAI;

namespace HannibalAI.Patches
{
    // NOTE: This patch is intentionally disabled to prevent duplicate processing
    // The BattleController is already properly registered as a mission behavior
    // in SubModule.cs, which handles the mission tick events automatically
    
    // [HarmonyPatch(typeof(Mission), "Tick")]
    public class BattleUpdatePatch
    {
        // This code is kept for reference but no longer used
        
        /*
        private static BattleController _battleController;

        static void Postfix(Mission __instance, float dt)
        {
            if (__instance != null && __instance.CombatType == Mission.MissionCombatType.Combat)
            {
                // We don't need to create a BattleController here anymore
                // The SubModule.cs already adds it as a proper mission behavior
                
                // Log that patch was executed if in debug mode
                if (ModConfig.Instance.Debug)
                {
                    Logger.Instance.Info("Battle mission tick via Harmony - no longer used");
                }
            }
        }
        */
    }
}