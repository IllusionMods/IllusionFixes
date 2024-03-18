using ChaCustom;
using HarmonyLib;
using UnityEngine.UI;

namespace IllusionFixes
{
    public partial class HeterochromiaFix
    {
        internal static class Hooks
        {
            // Fix "both" being selected in eye color when loaded card uses different colors per eye
#if KKS
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFileLimited), typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool))]
#else
            [HarmonyPostfix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFileLimited), typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool))]
#endif
            internal static void LoadFileLimitedPostfix(ChaFileControl __instance)
            {
                //Find the toggle set
                var cvsEye02 = CustomBase.Instance?.gameObject.GetComponentInChildren<CvsEye02>(true);
                if (cvsEye02 == null) return;

                var toggles = cvsEye02.tglEyeSetType;
                if (toggles == null) return;

                int pupil1 = __instance.custom.face.pupil[0].id;
                int pupil2 = __instance.custom.face.pupil[1].id;
                int gradient1 = __instance.custom.face.pupil[0].gradMaskId;
                int gradient2 = __instance.custom.face.pupil[1].gradMaskId;

                //If edit to both is selected, select edit to left so the character can load properly
                if (toggles[0].isOn && (pupil1 != pupil2 || gradient1 != gradient2))
                    toggles[1].isOn = true;
            }

            // Fix for the eyeline shadow color not being updated when changing skin color
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.CreateBodyTexture))]
            private static void CreateBodyTextureHook(ChaControl __instance) => __instance.UpdateEyelineShadowColor();
        }
    }
}
