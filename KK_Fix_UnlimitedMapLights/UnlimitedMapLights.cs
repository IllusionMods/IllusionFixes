using BepInEx;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Common;

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

        [HarmonyTranspiler, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.AddLight), new[] { typeof(int) })]
        public static IEnumerable<CodeInstruction> UnlimitedLights(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            codes[0].opcode = OpCodes.Nop;
            codes[1].opcode = OpCodes.Nop;
            codes[2].opcode = OpCodes.Nop;
            codes[3].opcode = OpCodes.Br;

            return codes;
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(Studio.LightLine), "CreateMaterial")]
        public static IEnumerable<CodeInstruction> LightLineFix(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            for(int i = 0; i < codes.Count; i++)
            {
                if(codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "Custom/LightLine")
                {
                    codes[i].operand = "Custom/LineShader";
                    break;
                }
            }

            return codes;
        }
    }
}
