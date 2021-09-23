using BepInEx;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    [BepInIncompatibility("keelhauled.cameratargetfix")]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class CameraTargetFix : CameraTargetFixCore
    {
        public const string GUID = "KKS_Fix_CameraTarget";

        protected override void Awake()
        {
            var set = Config.Bind("Controls", "Unlock cursor when Cam center is off", false,
                "By default cursor will be unlocked when the camera center point is hidden. This plugin stops that from happening, so the cursor is locked by default. Turn this on to restore the original behavior.\nChanges take effect after studio resteart.");

            if (!set.Value)
            {
                base.Awake();

                Harmony.Patch(typeof(Studio.CameraControl).GetMethod("LateUpdate", AccessTools.all),
                    transpiler: new HarmonyMethod(typeof(CameraTargetFixCore).GetMethod(nameof(StudioPatch), AccessTools.all)));
            }
        }
    }
}
