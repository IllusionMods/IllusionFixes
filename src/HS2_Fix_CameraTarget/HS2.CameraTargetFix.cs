using BepInEx;
using CharaCustom;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    [BepInIncompatibility("keelhauled.cameratargetfix")]
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class CameraTargetFix : CameraTargetFixCore
    {
        public const string GUID = "HS2_Fix_CameraTarget";

        protected override void Awake()
        {
            base.Awake();

            if (Paths.ProcessName == Constants.StudioProcessName)
            {
                Harmony.Patch(typeof(Studio.CameraControl).GetMethod("InternalUpdateCameraState", AccessTools.all),
                              transpiler: new HarmonyMethod(typeof(CameraTargetFixCore).GetMethod(nameof(StudioPatch), AccessTools.all)));
            }
            else
            {
                Harmony.Patch(typeof(CustomControl).GetMethod("Start", AccessTools.all),
                              postfix: new HarmonyMethod(GetType().GetMethod(nameof(MakerPatch), AccessTools.all)));
            }
        }

        private static void MakerPatch() => CustomBase.Instance.centerDraw = Manager.Config.CameraData.Look;
    }
}
