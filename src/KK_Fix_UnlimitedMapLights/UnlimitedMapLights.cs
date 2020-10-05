using BepInEx;
using Common;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class UnlimitedMapLights : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_UnlimitedMapLights";
        public const string PluginName = "Unlimited Map Lights";

        private void Awake() => Harmony.CreateAndPatchAll(GetType());

        [HarmonyPrefix, HarmonyPatch(typeof(Studio.SceneInfo), nameof(Studio.SceneInfo.AddLight))]
        private static bool UnlimitedLights() => false;

        [HarmonyTranspiler, HarmonyPatch(typeof(Studio.LightLine), "CreateMaterial")]
        private static IEnumerable<CodeInstruction> LightLineFix(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Ldstr && (string)code.operand == "Custom/LightLine")
                {
                    code.operand = "Custom/LineShader";
                    break;
                }
            }

            return instructions;
        }
    }
}
