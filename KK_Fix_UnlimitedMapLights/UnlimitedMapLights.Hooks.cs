using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace IllusionFixes
{
    public partial class UnlimitedMapLights
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneInfo))]
            [HarmonyPatch(nameof(Studio.SceneInfo.isLightCheck), MethodType.Getter)]
            public static bool UnlimitedLights(ref bool __result)
            {
                __result = true;
                return false;
            }

            [HarmonyTranspiler, HarmonyPatch(typeof(Studio.LightLine), "CreateMaterial")]
            public static IEnumerable<CodeInstruction> LightLineFix(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var code in instructions)
                {
                    if (code.opcode == OpCodes.Ldstr && (string)code.operand == "Custom/LightLine")
                        code.operand = "Custom/LineShader";

                    yield return code;
                }
            }
        }
    }
}
