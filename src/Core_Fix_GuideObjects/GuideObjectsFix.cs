using BepInEx;
using Common;
using HarmonyLib;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class GuideObjectsFix : BaseUnityPlugin
    {
        public const string GUID = "Fix_GuideObjects";
        public const string PluginName = "Guide Objects Fix";

        internal void Main() => Harmony.CreateAndPatchAll(typeof(Hooks));

        private static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(CustomUtility.CustomGuideBase), "Start")]
            internal static void CustomGuideBase_Start(CustomUtility.CustomGuideBase __instance, ref Color ___colorNormal, ref Color ___colorHighlighted)
            {
                if (__instance.gameObject.name == "X" || __instance.gameObject.name == "Y" || __instance.gameObject.name == "Z")
                {
                    ___colorNormal.a = 1f;
                    ___colorHighlighted.a = 1f;
                }
                else if (__instance.gameObject.name == "XYZ")
                {
                    ___colorNormal.r = 0f;
                    ___colorNormal.g = 0f;
                    ___colorNormal.b = 0f;
                    ___colorNormal.a = 0.25f;
                    ___colorHighlighted.r = 0f;
                    ___colorHighlighted.g = 0f;
                    ___colorHighlighted.b = 0f;
                    ___colorHighlighted.a = 0.5f;
                }

                //Render always on top
                __instance.material.shader = Shader.Find("Hidden/Internal-Colored");
                __instance.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                __instance.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                __instance.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                __instance.material.SetInt("_ZWrite", 0);
                __instance.material.SetInt("_ZTest", 0);
                __instance.material.color = ___colorNormal;

                if (__instance.transform.parent.name == "rotation")
                {
                    //Prevent z fighting
                    if (__instance.gameObject.name == "X")
                        __instance.material.renderQueue = __instance.material.renderQueue + 1;
                    else if (__instance.gameObject.name == "Y")
                        __instance.material.renderQueue = __instance.material.renderQueue + 2;
                    else if (__instance.gameObject.name == "Z")
                        __instance.material.renderQueue = __instance.material.renderQueue + 3;
                }
            }
        }
    }
}
