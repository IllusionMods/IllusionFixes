using BepInEx;
using ChaCustom;
using Harmony;
using Illusion.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace KK_Fix_MakerOptimizations
{
    public partial class MakerOptimizations
    {
        private static class Hooks
        {
            public static void Patch(HarmonyInstance harmony)
            {
                harmony.PatchAll(typeof(Hooks));

                SetupSetting(harmony,
                    typeof(CustomSelectInfoComponent).GetMethod("Disvisible", AccessTools.all),
                    typeof(Hooks).GetMethod(nameof(HarmonyPatch_CustomSelectInfoComponent_Disvisible), AccessTools.all),
                    DisableNewIndicator);

                SetupSetting(harmony,
                    typeof(CustomNewAnime).GetMethod("Update", AccessTools.all),
                    typeof(Hooks).GetMethod(nameof(HarmonyPatch_CustomNewAnime_Update), AccessTools.all),
                    DisableNewAnimation);

                if (DisableIKCalc.Value)
                {
                    var replace = typeof(CustomBase).GetMethod("UpdateIKCalc", AccessTools.all);
                    var prefix = typeof(Hooks).GetMethod(nameof(HarmonyPatch_CustomBase_UpdateIKCalc), AccessTools.all);
                    harmony.Patch(replace, new HarmonyMethod(prefix), null);
                }

                {
                    var replace = typeof(CustomScene).GetMethod("Start", AccessTools.all);
                    var prefix = typeof(Hooks).GetMethod(nameof(MakerStartHook), AccessTools.all);
                    harmony.Patch(replace, null, new HarmonyMethod(prefix));
                }
            }

            private static void SetupSetting(HarmonyInstance harmony, MethodInfo targetMethod, MethodInfo patchMethod, ConfigWrapper<bool> targetSetting)
            {
                if (targetSetting.Value)
                    harmony.Patch(targetMethod, new HarmonyMethod(patchMethod), null);

                targetSetting.SettingChanged += (sender, args) =>
                {
                    if (targetSetting.Value)
                        harmony.Patch(targetMethod, new HarmonyMethod(patchMethod), null);
                    else
                        harmony.RemovePatch(targetMethod, patchMethod);
                };
            }

            // Stop animation on new items
            private static bool HarmonyPatch_CustomNewAnime_Update() => false;

            // Disable indicator for new items
            private static void HarmonyPatch_CustomSelectInfoComponent_Disvisible(CustomSelectInfoComponent __instance) => __instance.objNew.SetActiveIfDifferent(false);

            // Disable heavy method with little use
            private static bool HarmonyPatch_CustomBase_UpdateIKCalc() => false;

            public static void MakerStartHook(CustomScene __instance) => __instance.StartCoroutine(OnMakerLoaded());

            private static IEnumerator OnMakerLoaded()
            {
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                // Put logic to run after maker is loaded here

                if (DisableHiddenTabs.Value)
                {
                    /* Tried and not working:
                     * treeGroup.interactable and treeItem.SetActive Doesn't do anything
                     * Splitting tabs into separate canvases makes fps worse but stable
                     * Disabling treeGroup.GetComponentsInChildren<UI_RaycastCtrl>() doesn't do much
                     * SetActive on top tab groups gives best fps but takes long to switch
                     * Changing tab parent out of canvas is same as setactive, canvas needs to recalculate everything
                     */

                    var kkKiyaseExists = GameObject.Find("KK_Kiyase") != null;
                    var treeTop = GameObject.Find("CvsMenuTree");

                    foreach (Transform mainTab in treeTop.transform)
                    {
                        var topMenuToggle = _canvasObjectLinks.TryGetValue(mainTab.name, out var topTabName)
                            ? GameObject.Find(topTabName)?.GetComponent<Toggle>()
                            : null;

                        var updateTabCallbacks = new List<Action>();
                        foreach (Transform subTab in mainTab)
                        {
                            var toggle = subTab.GetComponent<Toggle>();
                            if (toggle == null) continue;

                            var innerContent = subTab.Cast<Transform>().FirstOrDefault(x =>
                            {
                                // Needed for KK_Kiyase to not crash, it uses slides under this tab
                                if (kkKiyaseExists && x.GetComponent<CvsBreast>() != null) return false;

                                // Tab pages have raycast controllers on them, buttons have only image
                                return x.GetComponent<UI_RaycastCtrl>() != null;
                            })?.gameObject;
                            if (innerContent == null) continue;

                            void SetTabActive(bool val) => innerContent.SetActive(val && (topMenuToggle == null || topMenuToggle.isOn));

                            toggle.onValueChanged.AddListener(SetTabActive);
                            updateTabCallbacks.Add(() => SetTabActive(toggle.isOn));
                        }

                        topMenuToggle?.onValueChanged.AddListener(val =>
                        {
                            foreach (var callback in updateTabCallbacks)
                                callback();
                        });

                        foreach (var callback in updateTabCallbacks)
                            callback();
                    }
                }
            }

            /// <summary>
            /// Because Illusion can't make consistent names. There's probably a better way.
            /// </summary>
            private static readonly Dictionary<string, string> _canvasObjectLinks = new Dictionary<string, string>
            {
                {"00_FaceTop"      , "tglFace"       },
                {"01_BodyTop"      , "tglBody"       },
                {"02_HairTop"      , "tglHair"       },
                {"03_ClothesTop"   , "tglCoordinate" },
                {"04_AccessoryTop" , "tglAccessories"},
                {"05_ParameterTop" , "tglParameter"  },
                {"06_SystemTop"    , "tglSystem"     },
            };

            [HarmonyPostfix, HarmonyPatch(typeof(CustomControl), "Start")]
            public static void MakerUIHideLagFix(CustomControl __instance)
            {
                var t = typeof(CustomControl);
                var bf = BindingFlags.Instance | BindingFlags.NonPublic;

                var fi_HideFrontUI = t.GetField("_hideFrontUI", bf);
                var oldHideFrontUI = (BoolReactiveProperty)fi_HideFrontUI.GetValue(__instance);

                var newHideFrontUI = new BoolReactiveProperty(false);   //In today's episode of how the fuck do I unsubscribe from UniRx
                fi_HideFrontUI.SetValue(__instance, newHideFrontUI); //By replacing it of course!
                oldHideFrontUI.Dispose(); //Tune in next time!

                var cvsSpace = (Canvas)t.GetField("cvsSpace", bf).GetValue(__instance);
                var objFrontUIGroup = (GameObject)t.GetField("objFrontUIGroup", bf).GetValue(__instance);
                var cg = objFrontUIGroup.GetComponent<CanvasGroup>();

                //Modified code from CustomControl.Start -> _hideFrontUI.Subscribe anonymous method
                newHideFrontUI.Subscribe((hideFrontUI) =>
                {
                    if(__instance.saveMode) return;

                    //Instead of enabling/disabling the CanvasGroup Gameobject, just hide it and make it non-interactive
                    //This way we get the same effect but for no cost for load/unload
                    cg.alpha = hideFrontUI ? 0f : 1f;
                    cg.interactable = !hideFrontUI;
                    cg.blocksRaycasts = !hideFrontUI;

                    if(cvsSpace) cvsSpace.enabled = !hideFrontUI;
                    __instance.ChangeMainCameraRect(!hideFrontUI ? CustomControl.MainCameraMode.Custom : CustomControl.MainCameraMode.View);
                });
            }
        }
    }
}