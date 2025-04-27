using System;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace HannibalAI
{
    public class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                _harmony = new Harmony("com.hannibalai.mod");
                _harmony.PatchAll();
                TaleWorlds.Library.Debug.Print("[HannibalAI] Patches applied successfully.");
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[HannibalAI] Failed to apply patches: {ex.Message}");
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();

            try
            {
                _harmony?.UnpatchAll("com.hannibalai.mod");
                TaleWorlds.Library.Debug.Print("[HannibalAI] Patches removed successfully.");
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[HannibalAI] Failed to remove patches: {ex.Message}");
            }
        }
    }
}
