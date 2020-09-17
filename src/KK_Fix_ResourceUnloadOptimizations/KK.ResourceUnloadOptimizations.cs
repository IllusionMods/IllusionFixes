using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using Common;
using FBSAssist;
using HarmonyLib;
using Manager;
using UnityEngine;
using MissingMemberException = System.MissingMemberException;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class ResourceUnloadOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ResourceUnloadOptimizations";

        private void Start()
        {
            //var f = HarmonyLib.Tools.Logger.ChannelFilter;
            //HarmonyLib.Tools.Logger.ChannelFilter = HarmonyLib.Tools.Logger.LogChannel.All;
            Harmony.CreateAndPatchAll(typeof(AntiTrashHooks));
            //HarmonyLib.Tools.Logger.ChannelFilter = f;
        }

        private static class AntiTrashHooks
        {
            /// <summary>
            /// This hook fixes the mere existence of OnGUI code generating a ton of unnecessary garbage
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(GUILayoutUtility), "Begin")]
            private static IEnumerable<CodeInstruction> FixOnguiGarbageDump(IEnumerable<CodeInstruction> instructions,
                ILGenerator generator)
            {
                var luT = AccessTools.TypeByName("UnityEngine.GUILayoutUtility") ?? throw new MissingMemberException("AccessTools.TypeByName(\"UnityEngine.GUILayoutUtility\")");
                var lcT = luT.GetNestedType("LayoutCache", AccessTools.all) ?? throw new MissingMemberException("luT.GetNestedType(\"LayoutCache\", AccessTools.all)");
                var topF = AccessTools.Field(lcT, "topLevel") ?? throw new MissingMemberException("AccessTools.Field(lcT, \"topLevel\")");
                var winF = AccessTools.Field(lcT, "windows") ?? throw new MissingMemberException("AccessTools.Field(lcT, \"windows\")");
                var curF = AccessTools.Field(luT, "current") ?? throw new MissingMemberException("AccessTools.Field(luT, \"current\")");
                var lgF = AccessTools.Field(lcT, "layoutGroups") ?? throw new MissingMemberException("AccessTools.Field(lcT, \"layoutGroups\")");
                var lgT = AccessTools.TypeByName("UnityEngine.GUILayoutGroup") ?? throw new MissingMemberException("AccessTools.TypeByName(\"UnityEngine.GUILayoutGroup\")");
                var entrF = AccessTools.Field(lgT, "entries") ?? throw new MissingMemberException("AccessTools.Field(lgT, \"entries\")");
                var entrT = AccessTools.TypeByName("UnityEngine.GUILayoutEntry") ?? throw new MissingMemberException("AccessTools.TypeByName(\"UnityEngine.GUILayoutEntry\")");
                var entrClearM = AccessTools.Method(typeof(List<>).MakeGenericType(entrT), "Clear");

                var sltID = AccessTools.Method(luT, "SelectIDList");
                var curP = AccessTools.PropertyGetter(typeof(Event), "current");
                var typeP = AccessTools.PropertyGetter(typeof(Event), "type");

                var l0 = generator.DefineLabel();
                var l1 = generator.DefineLabel();
                var l2 = generator.DefineLabel();

                var replacementInstr = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Call, sltID),
                    new CodeInstruction(OpCodes.Stloc_0),
                    new CodeInstruction(OpCodes.Call, curP),
                    new CodeInstruction(OpCodes.Callvirt, typeP),
                    new CodeInstruction(OpCodes.Ldc_I4_8),
                    new CodeInstruction(OpCodes.Bne_Un_S, l0),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, topF),
                    new CodeInstruction(OpCodes.Brtrue_S, l1),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(lgT, new Type[0])),
                    new CodeInstruction(OpCodes.Stfld, topF),
                    new CodeInstruction(OpCodes.Ldloc_0) {labels = new List<Label> {l1}},
                    new CodeInstruction(OpCodes.Ldfld, topF),
                    new CodeInstruction(OpCodes.Ldfld, entrF),
                    new CodeInstruction(OpCodes.Callvirt, entrClearM),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, winF),
                    new CodeInstruction(OpCodes.Brtrue_S, l2),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(lgT, new Type[0])),
                    new CodeInstruction(OpCodes.Stfld, winF),
                    new CodeInstruction(OpCodes.Ldloc_0) {labels = new List<Label> {l2}},
                    new CodeInstruction(OpCodes.Ldfld, winF),
                    new CodeInstruction(OpCodes.Ldfld, entrF),
                    new CodeInstruction(OpCodes.Callvirt, entrClearM),
                    new CodeInstruction(OpCodes.Ldsfld, curF),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, topF),
                    new CodeInstruction(OpCodes.Stfld, topF),
                    new CodeInstruction(OpCodes.Ldsfld, curF),
                    new CodeInstruction(OpCodes.Ldfld, lgF),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Stack), nameof(Stack.Clear))),
                    new CodeInstruction(OpCodes.Ldsfld, curF),
                    new CodeInstruction(OpCodes.Ldfld, lgF),
                    new CodeInstruction(OpCodes.Ldsfld, curF),
                    new CodeInstruction(OpCodes.Ldfld, topF),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Stack), nameof(Stack.Push))),
                    new CodeInstruction(OpCodes.Ret),
                    new CodeInstruction(OpCodes.Ldsfld, curF) {labels = new List<Label> {l0}}
                };

                var instr = instructions.ToList();
                var c = 0;
                for (var i = instr.Count - 1; i >= 0; i--)
                {
                    c = c + Convert.ToInt32(instr[i].opcode == OpCodes.Ldsfld);
                    if (c == 3)
                    {
                        for (var j = i + 1; j < instr.Count; j++)
                            replacementInstr.Add(instr[j]);
                        break;
                    }
                }

                if (c != 3) throw new InvalidOperationException("IL footprint does not match expected?");
                UnityEngine.Debug.Log("IMGUI Patch done");
                return replacementInstr;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Scene), nameof(Scene.IsOverlap), MethodType.Getter)]
            private static bool GarbagelessOverlap(Scene.SceneStack<Scene.Data> ___sceneStack, ref bool __result)
            {
                __result = ___sceneStack.Count > 0 && ___sceneStack.Peek().isOverlap;
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Scene), nameof(Scene.LoadSceneName), MethodType.Getter)]
            private static bool GarbagelessLoadSceneName(Scene.SceneStack<Scene.Data> ___sceneStack, ref string __result)
            {
                var nameList = ___sceneStack.NowSceneNameList;
                __result = nameList[nameList.Count - 1];
                return false;
            }
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Scene), nameof(Scene.IsNowLoading), MethodType.Getter)]
            private static bool GarbagelessIsNowLoading(Stack<Scene.Data> ___loadStack, Scene.SceneStack<Scene.Data> ___sceneStack ,ref bool __result)
            {
                if (___loadStack.Count > 0)
                {
                    __result = true;
                    return false;
                }
                foreach (var data in ___sceneStack)
                {
                    if (data.isLoading)
                    {
                        __result = true;
                        break;
                    }
                    if (data.operation != null && !data.operation.isDone)
                    {
                        __result = true;
                        break;
                    }
                }
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
            /// Fix new Dictionary spam that caused massive allocations and garbage pileup
            /// </summary>
            private static readonly List<bool> _blendIdCache = new List<bool>();
            private static readonly List<float> _blendValueCache = new List<float>();
            [HarmonyPrefix]
            [HarmonyPatch(typeof(FBSBase), nameof(FBSBase.CalculateBlendShape))]
            public static bool CalculateBlendShape(FBSBase __instance, float ___correctOpenMax, float ___openRate,
                TimeProgressCtrl ___blendTimeCtrl, Dictionary<int, float> ___dictBackFace,
                Dictionary<int, float> ___dictNowFace)
            {
                if (__instance.FBSTarget.Length == 0) return false;

                var openMax = ___correctOpenMax >= 0f ? ___correctOpenMax : __instance.OpenMax;
                var lerpOpenRate = Mathf.Lerp(__instance.OpenMin, openMax, ___openRate);
                if (0f <= __instance.FixedRate) lerpOpenRate = __instance.FixedRate;

                var rate = 0f;
                if (___blendTimeCtrl != null) rate = ___blendTimeCtrl.Calculate();

                retry:
                try
                {
                    for (var index = 0; index < __instance.FBSTarget.Length; index++)
                    {
                        for (var i = 0; i < _blendIdCache.Count; i++)
                        {
                            _blendIdCache[i] = false;
                            _blendValueCache[i] = 0f;
                        }

                        var targetInfo = __instance.FBSTarget[index];
                        var skinnedMeshRenderer = targetInfo.GetSkinnedMeshRenderer();

                        var percent = (int)Mathf.Clamp(lerpOpenRate * 100f, 0f, 100f);

                        for (var j = 0; j < targetInfo.PtnSet.Length; j++)
                        {
                            var ptnSet = targetInfo.PtnSet[j];
                            var resultClose = 0f;
                            var resultOpen = 0f;

                            if (rate != 1f)
                            {
                                if (___dictBackFace.TryGetValue(j, out var valueBack))
                                {
                                    resultClose += valueBack * (100 - percent) * (1f - rate);
                                    resultOpen += valueBack * percent * (1f - rate);
                                }
                            }

                            if (___dictNowFace.TryGetValue(j, out var valueNow))
                            {
                                resultClose += valueNow * (100 - percent) * rate;
                                resultOpen += valueNow * percent * rate;
                            }

                            if (ptnSet.Close >= 0)
                            {
                                _blendIdCache[ptnSet.Close] = true;
                                _blendValueCache[ptnSet.Close] += resultClose;
                            }
                            if (ptnSet.Open >= 0)
                            {
                                _blendIdCache[ptnSet.Open] = true;
                                _blendValueCache[ptnSet.Open] += resultOpen;
                            }
                        }

                        for (var i = 0; i < _blendIdCache.Count; i++)
                        {
                            if (_blendIdCache[i])
                            {
                                skinnedMeshRenderer.SetBlendShapeWeight(i, _blendValueCache[i]);
                            }
                        }
                    }

                    return false;
                }
                catch (ArgumentOutOfRangeException)
                {
                    UnityEngine.Debug.Log("Increasing blend ID cache size");
                    _blendIdCache.AddRange(Enumerable.Repeat(false, 50));
                    _blendValueCache.AddRange(Enumerable.Repeat(0f, 50));
                    goto retry;
                }
            }
        }
    }
}