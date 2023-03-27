using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using Common;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, "AI_Fix_GarbageTruck", Version)]
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

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Scene), nameof(Scene.LoadSceneName), MethodType.Getter)]
            private static bool GarbagelessLoadSceneName(Scene __instance, out string __result)
            {
                var nameList = __instance.sceneStack.NowSceneNameList;
                // Always at least one
                __result = nameList[nameList.Count - 1];
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Scene), nameof(Scene.IsNowLoading), MethodType.Getter)]
            private static bool GarbagelessIsNowLoading(Scene __instance, ref bool __result)
            {
                if (__instance.loadStack.Count > 0)
                {
                    __result = true;
                }
                else
                {
                    // Uses struct GetEnumerator so no allocs, unlike .Any which does alloc.
                    foreach (var sceneData in __instance.sceneStack)
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
            private static bool GarbagelessIsOverlap(Scene __instance, out bool __result)
            {
                __result = __instance.sceneStack.Count > 0 && __instance.sceneStack.Peek().isOverlap;
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

            /// <summary>
            /// Reuse ChaFileControl when loading the character list to reduce garbage created
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyDebug]
            [HarmonyPatch(typeof(CharaCustom.CustomCharaFileInfoAssist), nameof(CharaCustom.CustomCharaFileInfoAssist.AddList))]
            private static IEnumerable<CodeInstruction> ReduceCreatedCharaListGarbage(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                // Find new object creation inside the for loop
                var cm = new CodeMatcher(instructions, generator)
                         .MatchForward(false,
                                       new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(AIChara.ChaFileControl))),
                                       new CodeMatch(OpCodes.Stloc_S))
                         .ThrowIfInvalid("ChaFileControl ctor not found");

                // Copy both of matched instructions and remove them 
                var a = cm.Instruction;
                cm.RemoveInstruction();
                var b = cm.Instruction;
                cm.RemoveInstruction();

                // Move the instructions outside of the for loop so new object is only created once and then reused
                cm.MatchBack(false,
                             new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(FolderAssist), nameof(FolderAssist.GetFileCount))),
                             new CodeMatch(OpCodes.Stloc_S))
                  .ThrowIfInvalid("GetFileCount not found")
                  .Advance(2)
                  .Insert(a, b);

                return cm.Instructions();
            }
        }
    }
}