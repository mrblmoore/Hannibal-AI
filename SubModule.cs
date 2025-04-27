using TaleWorlds.MountAndBlade;
using HarmonyLib;
using HannibalAI.Utils;

namespace HannibalAI
{
    public class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            _harmony = new Harmony("com.hannibalai.mod");
            _harmony.PatchAll();
            Logger.LogInfo("Hannibal AI SubModule loaded and Harmony patches applied.");
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
            if (_harmony != null)
            {
                _harmony.UnpatchAll("com.hannibalai.mod");
                Logger.LogInfo("Hannibal AI SubModule unloaded and Harmony patches removed.");
            }
        }
    }
}
