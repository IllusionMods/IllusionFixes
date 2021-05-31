using BepInEx;
using Common;
using HarmonyLib;
using UnityEngine;

// Fixed in KKS
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
            /// <summary>
            /// Fix accessory guide objects not having speed and scale sliders applied right after first enabling them
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomUtility.CustomGuideObject), "Awake")]
            internal static void CustomGuideObject_Awake(CustomUtility.CustomGuideObject __instance)
            {
                var id = __instance.name.EndsWith("1") ? 0 : 1;
                var css = Singleton<ChaCustom.CustomBase>.Instance.customSettingSave;
                __instance.scaleAxis = css.controllerScale[id];
                __instance.speedMove = css.controllerSpeed[id];
            }

            /// <summary>
            /// Fix accessory guide objects not being on top of everything like studio guide objects
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(CustomUtility.CustomGuideBase), "Start")]
            internal static void CustomGuideBase_Start(CustomUtility.CustomGuideBase __instance, ref Color ___colorNormal, ref Color ___colorHighlighted)
            {
                // Rotation guides have serious issues with z fighting
                if (__instance.transform.parent.name == "rotation")
                    return;

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
            }
        }
    }
}
