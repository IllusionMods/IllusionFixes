using BepInEx;
using Common;
using Harmony;
using Studio;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace KK_Fix_UnlimitedMapLights
{
    [BepInProcess("CharaStudio")]
    [BepInPlugin("keelhauled.unlimitedmaplights", "UnlimitedMapLights", Metadata.PluginsVersion)]
    public class UnlimitedMapLights : BaseUnityPlugin
    {
        private void Start()
        {
            var harmony = HarmonyInstance.Create("keelhauled.unlimitedmaplights.harmony");
            harmony.PatchAll(GetType());
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SceneInfo))]
        [HarmonyPatch(nameof(SceneInfo.isLightCheck), PropertyMethod.Getter)]
        public static bool UnlimitedLights(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(LightLine), "CreateMaterial")]
        public static IEnumerable<CodeInstruction> LightLineFix(IEnumerable<CodeInstruction> instructions)
        {
            foreach(var code in instructions)
            {
                if(code.opcode == OpCodes.Ldstr && (string)code.operand == "Custom/LightLine")
                    code.operand = "Custom/LineShader";

                yield return code;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DrawLightLine), nameof(DrawLightLine.OnPostRender))]
        public static bool HideLightLines()
        {
            return Studio.Studio.Instance.workInfo.visibleAxis;
        }
    }
}
