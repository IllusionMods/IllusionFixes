﻿using System;
using System.Collections.Generic;
using BepInEx;
using Common;
using HarmonyLib;
using ILSetUtility.TimeUtility;
using Manager;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, "HS2_Fix_GarbageTruck", Version)]
    public class GarbageTruck : BaseUnityPlugin
    {
        public const string GUID = "Fix_GarbageTruck";
        public const string Version = Constants.PluginsVersion;

        private void Start()
        {
            Harmony.CreateAndPatchAll(typeof(AntiGarbageHooks));
        }

        private static class AntiGarbageHooks
        {
            /// <summary>
            /// Runs many times per frame, heavy allocations because of linq
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.CameraControl), nameof(Studio.CameraControl.OnTriggerStay))]
            internal static bool OnTriggerStayPrefix(Collider other, List<Collider> ___listCollider, int ___m_MapLayer)
            {
                if (other == null)
                    return false;
                if ((___m_MapLayer & (1 << other.gameObject.layer)) == 0)
                    return false;
                if (!___listCollider.Contains(other))
                    ___listCollider.Add(other);
                return false;
            }

            /// <summary>
            /// Fix badly made ongui that crashes with the ongui garbage patch
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TimeUtilityDrawer), nameof(TimeUtilityDrawer.OnGUI))]
            private static bool FixBuiltinFps(TimeUtilityDrawer __instance)
            {
                GUI.Box(new Rect(4, 4, 100, 24), string.Empty, GUI.skin.box);
                GUI.Label(new Rect(7, 5, 100, 24), $"FPS:{__instance.fps:000.0}", __instance.style);
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Scene), nameof(Scene.LoadSceneName), MethodType.Getter)]
            private static bool GarbagelessLoadSceneName(out string __result)
            {
                var nameList = Scene._sceneStack.NowSceneNameList;
                // Always at least one
                __result = nameList[nameList.Count - 1];
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Scene), nameof(Scene.IsNowLoading), MethodType.Getter)]
            private static bool GarbagelessIsNowLoading(ref bool __result)
            {
                if (Scene._loadStack.Count > 0)
                {
                    __result = true;
                }
                else
                {
                    // Uses struct GetEnumerator so no allocs, unlike .Any which does alloc.
                    foreach (var sceneData in Scene._sceneStack)
                    {
                        if (sceneData.isLoading)
                        {
                            __result = true;
                            break;
                        }
                    }
                }
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Scene), nameof(Scene.IsOverlap), MethodType.Getter)]
            private static bool GarbagelessIsOverlap(out bool __result)
            {
                __result = Scene._overlapList.Count > 0;
                return false;
            }

            /// <summary>
            /// Prevent creating new HsvColor object and promptly discarding it on each call
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(HsvColor), nameof(HsvColor.ToRgb), typeof(float), typeof(float), typeof(float))]
            private static bool GarbagelessToRgb(ref Color __result, float h, float s, float v)
            {
                if (s == 0f)
                {
                    __result = new Color(v, v, v);
                    return false;
                }

                var num = h / 60f;
                var num3 = num - (float)Math.Floor(num);
                var num4 = v * (1f - s);
                var num5 = v * (1f - s * num3);
                var num6 = v * (1f - s * (1f - num3));
                switch ((int)Math.Floor(num) % 6)
                {
                    case 0:
                        __result = new Color(v, num6, num4);
                        break;
                    case 1:
                        __result = new Color(num5, v, num4);
                        break;
                    case 2:
                        __result = new Color(num4, v, num6);
                        break;
                    case 3:
                        __result = new Color(num4, num5, v);
                        break;
                    case 4:
                        __result = new Color(num6, num4, v);
                        break;
                    case 5:
                        __result = new Color(v, num4, num5);
                        break;
                    default:
                        return true;
                }

                return false;
            }
        }
    }
}