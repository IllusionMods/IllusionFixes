﻿using BepInEx.Configuration;
using ChaCustom;
using HarmonyLib;
using Illusion.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace IllusionFixes
{
    public partial class MakerOptimizations
    {
        internal static class Hooks
        {
            internal static void InstallHooks()
            {
                var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));

                SetupSetting(harmony,
                    typeof(CustomSelectInfoComponent).GetMethod("Disvisible", AccessTools.all),
                    typeof(Hooks).GetMethod(nameof(HarmonyPatch_CustomSelectInfoComponent_Disvisible), AccessTools.all),
                    DisableNewIndicator);

                SetupSetting(harmony,
                    typeof(CustomNewAnime).GetMethod("Update", AccessTools.all),
                    typeof(Hooks).GetMethod(nameof(HarmonyPatch_CustomNewAnime_Update), AccessTools.all),
                    DisableNewAnimation);

                {
                    var replace = typeof(CustomScene).GetMethod("Start", AccessTools.all);
                    var prefix = typeof(Hooks).GetMethod(nameof(MakerStartHook), AccessTools.all);
                    harmony.Patch(replace, null, new HarmonyMethod(prefix));
                }
            }

            private static void SetupSetting(Harmony harmony, MethodInfo targetMethod, MethodInfo patchMethod, ConfigEntry<bool> targetSetting)
            {
                if (targetSetting.Value)
                    harmony.Patch(targetMethod, new HarmonyMethod(patchMethod), null);

                targetSetting.SettingChanged += (sender, args) =>
                {
                    if (targetSetting.Value)
                        harmony.Patch(targetMethod, new HarmonyMethod(patchMethod), null);
                    else
                        harmony.Unpatch(targetMethod, patchMethod);
                };
            }

            // Stop animation on new items
            private static bool HarmonyPatch_CustomNewAnime_Update() => false;

            // Disable indicator for new items
            private static void HarmonyPatch_CustomSelectInfoComponent_Disvisible(CustomSelectInfoComponent __instance) => __instance.objNew.SetActiveIfDifferent(false);

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

#if KK
                    var kkKiyaseExists = GameObject.Find("KK_Kiyase") != null;
#endif
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
#if KK
                                // Needed for KK_Kiyase to not crash, it uses slides under this tab
                                if (kkKiyaseExists && x.GetComponent<CvsBreast>() != null) return false;
#endif

                                // Tab pages have raycast controllers on them, buttons have only image
                                return x.GetComponent<UI_RaycastCtrl>() != null;
                            })?.gameObject;
                            if (innerContent == null) continue;

#if KKS
                            // Needed in KKS because the close action is set in a coroutine which doesn't finish because we disable the gameobject
                            foreach (var window in innerContent.GetComponentsInChildren<CustomSelectWindow>())
                            {
                                window.btnClose.OnClickAsObservable().Subscribe(_ =>
                                {
                                    if (window.tglReference)
                                        window.tglReference.isOn = false;
                                });
                            }
#endif

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

            /// <summary>
            /// Based on MakerLag.dll by essu
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(CustomControl), "Start")]
            public static void MakerUiHideLagFix(CustomControl __instance)
            {
                var customControl = Traverse.Create(__instance);

                var hideFrontUi = customControl.Field("_hideFrontUI");
                var oldHide = hideFrontUi.GetValue<BoolReactiveProperty>();
                oldHide.Dispose();
                var newHide = new BoolReactiveProperty(false);
                hideFrontUi.SetValue(newHide);

                var cvsSpace = customControl.Field("cvsSpace").GetValue<Canvas>();
                var objFrontUiGroup = customControl.Field("objFrontUIGroup").GetValue<GameObject>();
                var frontCanvasGroup = objFrontUiGroup.GetComponent<CanvasGroup>();

                //Modified code from CustomControl.Start -> _hideFrontUI.Subscribe anonymous method
                newHide.Subscribe(hideFront =>
                {
                    if (__instance.saveMode) return;

                    //Instead of enabling/disabling the CanvasGroup Gameobject, just hide it and make it non-interactive
                    //This way we get the same effect but for no cost for load/unload
                    frontCanvasGroup.alpha = hideFront ? 0f : 1f;
                    frontCanvasGroup.interactable = !hideFront;
                    frontCanvasGroup.blocksRaycasts = !hideFront;

                    if (cvsSpace) cvsSpace.enabled = !hideFront;
                    __instance.ChangeMainCameraRect(!hideFront ? CustomControl.MainCameraMode.Custom : CustomControl.MainCameraMode.View);
                });
            }

            //Fix adapted from Patchwork
            /// <summary>
            /// Setting the parent to null will prevent the code from setting the transform's parent. The code sets the parent too early which causes extemely slow load times in the character maker.
            /// The parent is passed to the postfix in the __state where it will be set after the code is finished
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(SaveFrameAssist), nameof(SaveFrameAssist.CreateSaveFrameToHierarchy))]
            public static void CreateSaveFrameToHierarchyPrefix(ref Transform parent, ref Transform __state)
            {
                __state = parent;
                parent = null;
            }
            /// <summary>
            /// Set the parent after the code, prefix prevents it from being set in the code
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(SaveFrameAssist), nameof(SaveFrameAssist.CreateSaveFrameToHierarchy))]
            public static void CreateSaveFrameToHierarchyPostfix(ref Transform __state, GameObject ___objSaveFrameTop)
            {
                if ((bool)__state)
                    ___objSaveFrameTop.transform.parent = __state;
            }

            public static string lastAnimeStateName;
            public static bool UpdateIKCalc_manualRun = true;

#if KK
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
            private static void ChangeCoordinateTypePrefix()
            {
                UpdateIKCalc_manualRun = true;
            }
#elif EC
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeNowCoordinate), typeof(ChaFileCoordinate), typeof(bool), typeof(bool))]
            private static void ChangeNowCoordinatePrefix()
            {
                UpdateIKCalc_manualRun = true;
            }
#endif

            [HarmonyPrefix, HarmonyPatch(typeof(CustomBase), nameof(CustomBase.UpdateIKCalc))]
            public static bool FixIKSpam(CustomBase __instance)
            {
                // Disable IK updates in maker to prevent chicken chikas when using ABMX modifiers
                if (DisableIKCalc.Value) return false;

                if (__instance.motionIK != null && (lastAnimeStateName != __instance.animeStateName | UpdateIKCalc_manualRun))
                {
                    UpdateIKCalc_manualRun = false;
                    lastAnimeStateName = __instance.animeStateName;
                    __instance.motionIK.Calc(__instance.animeStateName);
                }

                return false;
            }

            /// Dick starts flopping around in free H with this patch, otherwise would have been better than above
            //private static Dictionary<MotionIK, string> previousStates = new Dictionary<MotionIK, string>();
            //[HarmonyPrefix, HarmonyPatch(typeof(MotionIK), nameof(MotionIK.Calc))]
            //public static bool FixIKSpam(MotionIK __instance, ref string stateName)
            //{
            //    if(previousStates.TryGetValue(__instance, out var prevState))
            //    {
            //        if(stateName != prevState)
            //        {
            //            previousStates[__instance] = stateName;
            //            Console.WriteLine(stateName);
            //            return true;
            //        }
            //    }
            //    else
            //    {
            //        previousStates.Add(__instance, stateName);
            //        return true;
            //    }

            //    return false;
            //}

#if EC
            [HarmonyPostfix, HarmonyPatch(typeof(CvsClothes), "UpdateSelectClothes")]
            public static void HarmonyPatch_CvsClothes_UpdateSelectClothes()
            {
                if (ClothesAssetUnload.Value)
                {
                    UnityEngine.Resources.UnloadUnusedAssets();
                    GC.Collect();
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), "FuncUpdateAccessory")]
            public static void HarmonyPatch_CvsAccessory_FuncUpdateAccessory()
            {
                if (ClothesAssetUnload.Value)
                {
                    UnityEngine.Resources.UnloadUnusedAssets();
                    GC.Collect();
                }
            }
#endif
        }
    }
}