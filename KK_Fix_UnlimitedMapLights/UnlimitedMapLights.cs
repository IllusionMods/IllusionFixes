using BepInEx;
using Harmony;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace KK_Fix_UnlimitedMapLights
{
    [BepInProcess("CharaStudio")]
    [BepInPlugin("keelhauled.unlimitedmaplights", "UnlimitedMapLights", "1.0.0")]
    public class UnlimitedMapLights : BaseUnityPlugin
    {
        void Start()
        {
            var harmony = HarmonyInstance.Create("keelhauled.unlimitedmaplights.harmony");
            harmony.PatchAll(GetType());
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneInfo))]
        [HarmonyPatch(nameof(Studio.SceneInfo.isLightCheck), PropertyMethod.Getter)]
        public static bool UnlimitedLights(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(Studio.LightLine), "CreateMaterial")]
        public static IEnumerable<CodeInstruction> LightLineFix(IEnumerable<CodeInstruction> instructions)
        {
            foreach(var code in instructions)
            {
                if(code.opcode == OpCodes.Ldstr && (string)code.operand == "Custom/LightLine")
                    code.operand = "Custom/LineShader";

                yield return code;
            }
        }
    }
}
