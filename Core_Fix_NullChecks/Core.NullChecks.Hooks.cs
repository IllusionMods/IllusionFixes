using Common;
using HarmonyLib;

namespace IllusionFixes
{
    public partial class NullChecks
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
            internal static void ChangeSettingHairColor(int parts, ChaControl __instance) => RemoveNullParts(__instance.GetCustomHairComponent(parts));

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
            internal static void ChangeSettingHairOutlineColor(int parts, ChaControl __instance) => RemoveNullParts(__instance.GetCustomHairComponent(parts));

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairAcsColor))]
            internal static void ChangeSettingHairAcsColor(int parts, ChaControl __instance) => RemoveNullParts(__instance.GetCustomHairComponent(parts));

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateAccessoryMoveFromInfo))]
            internal static void UpdateAccessoryMoveFromInfo(int slotNo, ChaControl __instance) => RemoveNullParts(__instance.GetAccessory(slotNo)?.gameObject.GetComponent<ChaCustomHairComponent>());

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
