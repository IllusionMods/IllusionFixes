using BepInEx;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    [BepInIncompatibility("keelhauled.cameratargetfix")]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.StudioProcessName32bit)]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class CameraTargetFix : CameraTargetFixCore
    {
        public const string GUID = "PH_Fix_CameraTarget";

        protected override void Awake()
        {
            base.Awake();

            Harmony.Patch(typeof(Studio.CameraControl).GetMethod("LateUpdate", AccessTools.all),
                          transpiler: new HarmonyMethod(typeof(CameraTargetFixCore).GetMethod(nameof(StudioPatch), AccessTools.all)));
        }
    }
}
