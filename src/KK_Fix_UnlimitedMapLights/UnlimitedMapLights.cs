using BepInEx;
using Common;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Logging;
using BepInEx.Configuration;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class UnlimitedMapLights : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_UnlimitedMapLights";
        public const string PluginName = "Unlimited Map Lights";

        private static new ManualLogSource Logger;

        private static ConfigEntry<bool> showWarning;

        private void Awake()
        {
            Logger = base.Logger;

            showWarning = Config.Bind("Logging", "Show Maplight Warning", true, "Toggle the Warning that gets displayed if more than two maplights are added to the scene");

            Harmony.CreateAndPatchAll(typeof(UnlimitedMapLights));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Studio.SceneInfo), nameof(Studio.SceneInfo.AddLight))]
        private static void UnlimitedLights(Studio.SceneInfo __instance)
        {
            if(__instance.lightCount > 2 && showWarning.Value)
                Logger.LogMessage("Warning: Lights above 2 might not affect characters and some items!");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Studio.SceneInfo), nameof(Studio.SceneInfo.isLightCheck), MethodType.Getter)]
        private static void UnlimitedLights(ref bool __result) => __result = true;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Studio.SceneInfo), nameof(Studio.SceneInfo.isLightLimitOver), MethodType.Getter)]
        private static void UnlimitedLights2(ref bool __result) => __result = false;

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
