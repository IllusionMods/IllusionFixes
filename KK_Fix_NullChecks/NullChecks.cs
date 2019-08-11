using BepInEx;
using BepInEx.Harmony;
using Common;
using HarmonyLib;

namespace KK_Fix_NullChecks
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class NullChecks : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.cutscenelockupfix";
        public const string PluginName = "Null Checks";

        public void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }

        private static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
            public static void ChangeSettingHairColor(int parts, ChaControl __instance) => RemoveNullParts(__instance.GetCustomHairComponent(parts));

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
            public static void ChangeSettingHairOutlineColor(int parts, ChaControl __instance) => RemoveNullParts(__instance.GetCustomHairComponent(parts));

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairAcsColor))]
            public static void ChangeSettingHairAcsColor(int parts, ChaControl __instance) => RemoveNullParts(__instance.GetCustomHairComponent(parts));

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateAccessoryMoveFromInfo))]
            public static void UpdateAccessoryMoveFromInfo(int slotNo, ChaControl __instance) => RemoveNullParts(__instance.GetAccessory(slotNo)?.gameObject.GetComponent<ChaCustomHairComponent>());

            private static void RemoveNullParts(ChaCustomHairComponent hairComponent)
            {
                if (hairComponent == null)
                    return;

                hairComponent.rendAccessory = hairComponent.rendAccessory.RemoveNulls();
                hairComponent.rendHair = hairComponent.rendHair.RemoveNulls();
                hairComponent.trfLength = hairComponent.trfLength.RemoveNulls();
            }
        }
    }
}
