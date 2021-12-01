using BepInEx;
using BepInEx.Logging;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    [BepInPlugin(PluginGUID, PluginName, Constants.PluginsVersion)]
    public class FaceShadowsFix : BaseUnityPlugin
    {
        public const string PluginGUID = "Fix_FaceShadows";
        public const string PluginName = "Face Shadows Fix";
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        internal static class Hooks
        {
            //Adjust shadows to be consistent with Koikatsu
            [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeHeadNoAsync))]
            internal static void ChangeHeadNoAsync(ChaControl __instance)
            {
                //Allows the face to receive shadows
                __instance.rendFace.sharedMaterial.SetFloat("_FaceNormalG", 0);
                //Disable receive shadows for eye parts
                __instance.rendEye[0].receiveShadows = false;
                __instance.rendEye[1].receiveShadows = false;
                __instance.rendEyeW[0].receiveShadows = false;
                __instance.rendEyeW[1].receiveShadows = false;
                __instance.rendEyelineUp.receiveShadows = false;
                __instance.rendEyelineDown.receiveShadows = false;
            }
        }
    }
}
