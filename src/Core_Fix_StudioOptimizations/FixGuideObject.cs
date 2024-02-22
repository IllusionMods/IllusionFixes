using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Studio;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

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
                //Initialize rotation so that IKs with invalid rotation follow the parent's rotation
                __instance.transformTarget.localRotation = Quaternion.identity;
            }
        }
    }
}
