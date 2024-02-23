using BepInEx;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace IllusionFixes
{
    public partial class StudioOptimizations : BaseUnityPlugin
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Studio.GuideObject), nameof(Studio.GuideObject.CalcRotation))]
        private static void GuideObjectCalcRotationPrefix(Studio.GuideObject __instance)
        {
            if( !__instance.m_Enables[1] && __instance.transformTarget != null && __instance.mode == GuideObject.Mode.LocalIK )
            {
                //Initialize rotation so that IKs with disabled rotation follow the parent's rotation
                __instance.transformTarget.localRotation = Quaternion.identity;
            }
        }
    }
}
