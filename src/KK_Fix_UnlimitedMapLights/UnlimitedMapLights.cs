using BepInEx;
using BepInEx.Harmony;
using Common;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace IllusionFixes
{
    [BepInProcess("CharaStudio")]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class UnlimitedMapLights : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_UnlimitedMapLights";
        public const string PluginName = "Unlimited Map Lights";

        private void Awake() => HarmonyWrapper.PatchAll(GetType());

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneInfo), nameof(Studio.SceneInfo.AddLight))]
        private static bool UnlimitedLights() => false;

        [HarmonyTranspiler, HarmonyPatch(typeof(Studio.LightLine), "CreateMaterial")]
        private static IEnumerable<CodeInstruction> LightLineFix(IEnumerable<CodeInstruction> instructions)
        {
            foreach(var code in instructions)
            {
                if(code.opcode == OpCodes.Ldstr && (string)code.operand == "Custom/LightLine")
                {
                    code.operand = "Custom/LineShader";
                    break;
                }
            }

            return instructions;
        }
    }
}
