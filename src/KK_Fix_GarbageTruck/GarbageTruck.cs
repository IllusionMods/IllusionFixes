using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using Common;
using HarmonyLib;
using Illusion.Component.Correct;
using Illusion.Component.Correct.Process;
using ILSetUtility.TimeUtility;
using Manager;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, GUID, Constants.PluginsVersion)]
    public class GarbageTruck : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_GarbageTruck";

        private void Awake()
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

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(EyeLookCalc), nameof(EyeLookCalc.EyeUpdateCalc))]
            private static IEnumerable<CodeInstruction> FixUpdateCalcGarbage(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var c = new CodeMatcher(instructions, generator);
                // Remove unnecessary new object creation that gets immediately overwritten
                c.MatchForward(false,
                    new CodeMatch(OpCodes.Newobj),
                    new CodeMatch(OpCodes.Stloc_1),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld));

                if (c.IsInvalid) return instructions;

                return c
                    .RemoveInstruction()
                    .RemoveInstruction()
                    .Instructions();
            }

            /// <summary>
            /// Replace method to get rid of creating many new collections
            /// </summary>
            private static readonly ChaFileDefine.SiruParts[] _siruParts = {
                ChaFileDefine.SiruParts.SiruFrontUp,
                ChaFileDefine.SiruParts.SiruFrontDown,
                ChaFileDefine.SiruParts.SiruBackUp,
                ChaFileDefine.SiruParts.SiruBackDown
            };
            private static readonly MethodInfo _siruNewM = AccessTools.Property(typeof(ChaControl), "siruNewLv").GetGetMethod(true);
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), "UpdateSiru")]
            private static bool UpdateSiru(ChaControl __instance, ref bool __result, bool forceChange)
            {
                if (!__instance.hiPoly)
                {
                    return true;
                }

                var siruNewLv = (byte[])_siruNewM.Invoke(__instance, null);

                if (__instance.customMatFace != null)
                {
                    if (forceChange || __instance.fileStatus.siruLv[0] != siruNewLv[0])
                    {
                        __instance.fileStatus.siruLv[0] = siruNewLv[0];
                        __instance.customMatFace.SetFloat(ChaShader._liquidface, __instance.fileStatus.siruLv[0]);
                    }
                }

                if (__instance.customMatBody != null)
                {
                    var anyChanged = false;
                    for (var i = 0; i < _siruParts.Length; i++)
                    {
                        if (forceChange || __instance.fileStatus.siruLv[(int)_siruParts[i]] != siruNewLv[(int)_siruParts[i]])
                        {
                            anyChanged = true;
                            __instance.fileStatus.siruLv[(int)_siruParts[i]] = siruNewLv[(int)_siruParts[i]];
                        }
                    }
                    if (anyChanged)
                    {
                        __instance.customMatBody.SetFloat(ChaShader._liquidftop, __instance.fileStatus.siruLv[(int)_siruParts[0]]);
                        __instance.customMatBody.SetFloat(ChaShader._liquidfbot, __instance.fileStatus.siruLv[(int)_siruParts[1]]);
                        __instance.customMatBody.SetFloat(ChaShader._liquidbtop, __instance.fileStatus.siruLv[(int)_siruParts[2]]);
                        __instance.customMatBody.SetFloat(ChaShader._liquidbbot, __instance.fileStatus.siruLv[(int)_siruParts[3]]);

                        UpdateClothesSiru(__instance, 0);
                        UpdateClothesSiru(__instance, 1);
                        UpdateClothesSiru(__instance, 2);
                        UpdateClothesSiru(__instance, 3);
                        UpdateClothesSiru(__instance, 5);
                    }
                }

                __result = true;
                return false;
            }
            private static void UpdateClothesSiru(ChaControl __instance, int j)
            {
                __instance.UpdateClothesSiru(j,
                    __instance.fileStatus.siruLv[(int)_siruParts[0]],
                    __instance.fileStatus.siruLv[(int)_siruParts[1]],
                    __instance.fileStatus.siruLv[(int)_siruParts[2]],
                    __instance.fileStatus.siruLv[(int)_siruParts[3]]);
            }

            /// <summary>
            /// Fix badly made ongui that crashes with the ongui garbage patch
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TimeUtilityDrawer), "OnGUI")]
            private static bool FixBuiltinFps(TimeUtilityDrawer __instance, GUIStyle ___style)
            {
                var fps = (float)AccessTools.PropertyGetter(typeof(TimeUtilityDrawer), "fps").Invoke(__instance, null);
                GUI.Box(new Rect(4, 4, 100, 24), "", GUI.skin.box);
                GUI.Label(new Rect(7, 5, 100, 24), "FPS:" + fps.ToString("000.0"), ___style);
                return false;
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
            private static bool GarbagelessIsNowLoading(Stack<Scene.Data> ___loadStack, Scene.SceneStack<Scene.Data> ___sceneStack, ref bool __result)
            {
                if (___loadStack.Count > 0)
                {
                    __result = true;
                    return false;
                }
                foreach (var data in ___sceneStack) // Can't escape this one
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
                FBSAssist.TimeProgressCtrl ___blendTimeCtrl, Dictionary<int, float> ___dictBackFace,
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

            /// <summary>
            /// Allocation-free reimplementation (this can be called ~60 times per frame).
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaseProcess), nameof(BaseProcess.data), MethodType.Getter)]
            private static bool GetDataNoAlloc(BaseProcess __instance, ref BaseData ____data, ref BaseData __result)
            {
                if (____data == null)
                {
                    ____data = __instance.GetComponent<BaseData>();
                }
                __result = ____data;
                return false;
            }

            /// <summary>
            /// Use a preallocated buffer instead of doing new float[1024].
            /// </summary>
            private static readonly float[] _waveBuffer = new float[1024];
            private static float[] GetWaveBuffer()
            {
                Array.Clear(_waveBuffer, 0, _waveBuffer.Length);
                return _waveBuffer;
            }
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(FBSAssist.AudioAssist), nameof(FBSAssist.AudioAssist.GetAudioWaveValue))]
            private static IEnumerable<CodeInstruction> FixGetAudioWaveValue(IEnumerable<CodeInstruction> insts)
            {
                return new CodeMatcher(insts)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldc_I4, 1024),
                        new CodeMatch(OpCodes.Newarr, typeof(float)))
                    .ThrowIfInvalid("No match")
                    .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(AntiGarbageHooks), nameof(GetWaveBuffer)))
                    .RemoveInstruction()
                    .Instructions();
            }

            /// <summary>
            /// Use a nonallocating equality comparer for ChaReference.dictRefObj.
            /// </summary>
            [HarmonyPatch(typeof(ChaReference), MethodType.Constructor)]
            [HarmonyPostfix]
            [HarmonyPriority(Priority.First)]
            private static void FixChaReferenceComparer(ChaReference __instance)
            {
                var dic = new Dictionary<ChaReference.RefObjKey, GameObject>(
                    __instance.dictRefObj,
                    new RefObjKeyEqualityComparer());
                __instance.dictRefObj = dic;
            }
            class RefObjKeyEqualityComparer : IEqualityComparer<ChaReference.RefObjKey>
            {
                public bool Equals(ChaReference.RefObjKey x, ChaReference.RefObjKey y)
                {
                    return x == y;
                }

                public int GetHashCode(ChaReference.RefObjKey x)
                {
                    return ((int)x).GetHashCode();
                }
            }

            /// <summary>
            /// Cache allocations of many small arrays in ChaControl.UpdateVisible.
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateVisible))]
            private static IEnumerable<CodeInstruction> FixUpdateVisible(IEnumerable<CodeInstruction> insts)
            {
                var matcher = new CodeMatcher(insts);
                var get1DArray = AccessTools.Method(typeof(AntiGarbageHooks), nameof(Get1DArrayForUpdateVisible));
                var get2DArray = AccessTools.Method(typeof(AntiGarbageHooks), nameof(Get2DArrayForUpdateVisible));
                var array2DTypes = new Dictionary<Type, Type> {
                    { typeof(byte[,]), typeof(byte) },
                    { typeof(bool[,]), typeof(bool) },
                    { typeof(ChaReference.RefObjKey[,]), typeof(ChaReference.RefObjKey) },
                };
                int id = 0;
                matcher
                    .MatchForward(true, new CodeMatch(OpCodes.Newarr))
                    .Repeat(m => {
                        var elementType = m.Instruction.operand as Type;
                        m
                            .RemoveInstruction()
                            .Insert(
                                new CodeInstruction(OpCodes.Ldc_I4, id),
                                new CodeInstruction(OpCodes.Ldtoken, elementType),
                                new CodeInstruction(OpCodes.Call, get1DArray));
                        id++;
                    })
                    .Start()
                    .MatchForward(true,
                        new CodeMatch(inst =>
                            inst.opcode == OpCodes.Newobj &&
                            inst.operand is ConstructorInfo constructor &&
                            array2DTypes.ContainsKey(constructor.DeclaringType)))
                    .Repeat(m => {
                        var elementType = array2DTypes[(m.Instruction.operand as ConstructorInfo).DeclaringType];
                        m
                            .RemoveInstruction()
                            .Insert(
                                new CodeInstruction(OpCodes.Ldc_I4, id),
                                new CodeInstruction(OpCodes.Ldtoken, elementType),
                                new CodeInstruction(OpCodes.Call, get2DArray));
                        id++;
                    });
                if (id != _updateVisibleAllocationCount)
                {
                    Utilities.Logger.LogWarning($"Unexpected number of array allocations in UpdateVisible ({id}). Not patching.");
                    return insts;
                }
                Array.Clear(_updateVisibleArrays, 0, _updateVisibleAllocationCount);
                return matcher.Instructions();
            }
            private static readonly int _updateVisibleAllocationCount = 45;
            private static Array[] _updateVisibleArrays = new Array[_updateVisibleAllocationCount];
            private static Array Get1DArrayForUpdateVisible(int size, int id, RuntimeTypeHandle typeHandle)
            {
                var arr = _updateVisibleArrays[id];
                if (arr != null)
                {
                    Array.Clear(arr, 0, size);
                    return arr;
                }

                arr = Array.CreateInstance(Type.GetTypeFromHandle(typeHandle), size);
                _updateVisibleArrays[id] = arr;
                return arr;
            }
            private static Array Get2DArrayForUpdateVisible(int size0, int size1, int id, RuntimeTypeHandle typeHandle)
            {
                var arr = _updateVisibleArrays[id];
                if (arr != null)
                {
                    Array.Clear(arr, 0, size0 * size1);
                    return arr;
                }

                arr = Array.CreateInstance(Type.GetTypeFromHandle(typeHandle), size0, size1);
                _updateVisibleArrays[id] = arr;
                return arr;
            }
        }
    }
}
