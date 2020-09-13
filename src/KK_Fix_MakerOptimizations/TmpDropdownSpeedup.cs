using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Illusion.Extensions;
using TMPro;
using UnityEngine;

namespace IllusionFixes
{
    public partial class MakerOptimizations
    {
        private static class TmpDropdownSpeedup
        {
            public static void ApplyHooks()
            {
                Harmony.CreateAndPatchAll(typeof(TmpDropdownSpeedup));
            }

            private static readonly Dictionary<TMP_Dropdown, List<string>> _itemLookup = new Dictionary<TMP_Dropdown, List<string>>();

            [HarmonyPrefix, HarmonyPatch(typeof(TMP_Dropdown), nameof(TMP_Dropdown.Show))]
            private static void ShowPatch(TMP_Dropdown __instance, GameObject ___m_Dropdown, GameObject ___m_Blocker)
            {
                if (___m_Dropdown != null && _itemLookup.TryGetValue(__instance, out var previousItems))
                {
                    // Need to recreate the list if the items changed
                    if (!previousItems.SequenceEqual(__instance.options.Select(x => x.text)))
                    {
                        DestroyImmediate(___m_Dropdown);
                        DestroyImmediate(___m_Blocker);
                    }
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(TMP_Dropdown), nameof(TMP_Dropdown.Show))]
            private static void ShowPatch2(TMP_Dropdown __instance, GameObject ___m_Dropdown, GameObject ___m_Blocker)
            {
                var canBeShown = __instance.IsActive() && __instance.IsInteractable();
                if (canBeShown && !___m_Dropdown.activeSelf)
                {
                    ___m_Dropdown.SetActive(true);
                    ___m_Blocker.SetActive(true);
                    var m = Traverse.Create(__instance).Method("AlphaFadeList", new[] { typeof(float), typeof(float), typeof(float) });
                    m.GetValue(0.15f, 0f, 1f);

                    _itemLookup[__instance] = __instance.options.Select(x => x.text).ToList();
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(TMP_Dropdown), "DelayedDestroyDropdownList")]
            private static bool HidePatch(TMP_Dropdown __instance, GameObject ___m_Dropdown, float delay, ref IEnumerator __result)
            {
                IEnumerator DelayedDisable()
                {
                    yield return new WaitForSecondsRealtime(delay);
                    ___m_Dropdown.SetActiveIfDifferent(false);
                }
                __result = DelayedDisable();
                return false;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(TMP_Dropdown), nameof(TMP_Dropdown.IsExpanded), MethodType.Getter)]
            private static void IsExpandedPatch(ref bool __result, GameObject ___m_Dropdown)
            {
                if (___m_Dropdown != null)
                    __result = ___m_Dropdown.activeSelf;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(TMP_Dropdown), nameof(TMP_Dropdown.Hide))]
            private static void HidePatch2(ref GameObject ___m_Blocker, out GameObject __state)
            {
                __state = ___m_Blocker;
                ___m_Blocker = null; // Prevent blocker from being destroyed by Hide, restore it after
            }
            [HarmonyPostfix, HarmonyPatch(typeof(TMP_Dropdown), nameof(TMP_Dropdown.Hide))]
            private static void HidePatch3(ref GameObject ___m_Blocker, GameObject __state)
            {
                ___m_Blocker = __state;
                ___m_Blocker.SetActive(false);
            }
        }
    }
}