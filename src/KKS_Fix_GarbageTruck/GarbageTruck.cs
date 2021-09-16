using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using Common;
using FBSAssist;
using HarmonyLib;
using Illusion.Component.Correct.Process;
using ILSetUtility.TimeUtility;
using KKAPI.Utilities;
using Manager;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, GUID, Constants.PluginsVersion)]
    public partial class GarbageTruck : BaseUnityPlugin
    {
        public const string GUID = "KKS_Fix_GarbageTruck";

        private void Start()
        {
            Harmony.CreateAndPatchAll(typeof(AntiGarbageHooks));

            StartCoroutine(AntiGarbageHooks.BaseProcessUpdateJobCo());
        }

        private static class AntiGarbageHooks
        {
            // todo no studio yet
            ///// <summary>
            ///// Runs many times per frame, heavy allocations because of linq
            ///// </summary>
            //[HarmonyPrefix, HarmonyPatch(typeof(Studio.CameraControl), "OnTriggerStay")]
            //internal static bool OnTriggerStayPrefix(Collider other, List<Collider> ___listCollider, int ___m_MapLayer)
            //{
            //    if (!(other == null) && (___m_MapLayer & (1 << other.gameObject.layer)) != 0)
            //        if (!___listCollider.Contains(other))
            //            ___listCollider.Add(other);
            //    return false;
            //}

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
            /// Fix badly made ongui that crashes with the ongui garbage patch
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TimeUtilityDrawer), "OnGUI")]
            private static bool FixBuiltinFps(TimeUtilityDrawer __instance, GUIStyle ___style)
            {
                GUI.Box(new Rect(4, 4, 100, 24), "", GUI.skin.box);
                GUI.Label(new Rect(7, 5, 100, 24), "FPS:" + __instance.fps.ToString("000.0"), ___style);
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Scene), nameof(Scene.LoadSceneName), MethodType.Getter)]
            private static bool GarbagelessLoadSceneName(ref string __result)
            {
                var nameList = Scene._sceneStack.NowSceneNameList;
                __result = nameList[nameList.Count - 1];
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
            /// Use a preallocated buffer instead of doing new float[1024].
            /// </summary>
            private static readonly float[] _waveBuffer = new float[1024];



            private static float[] GetWaveBuffer()
            {
                Array.Clear(_waveBuffer, 0, _waveBuffer.Length);
                return _waveBuffer;
            }
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(AudioAssist), nameof(FBSAssist.AudioAssist.GetAudioWaveValue))]
            private static IEnumerable<CodeInstruction> FixGetAudioWaveValue(IEnumerable<CodeInstruction> insts)
            {
                return new CodeMatcher(insts)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldc_I4, 1024),
                        new CodeMatch(OpCodes.Newarr, typeof(float)))
                    .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(AntiGarbageHooks), nameof(GetWaveBuffer)))
                    .RemoveInstruction()
                    .Instructions();
            }

            #region Fix BaseProcessLateUpdate StartCoroutine allocations

            /// <summary>
            /// Spawning a new coroutine every time is very expensive in cpu and gc
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(BaseProcess), nameof(BaseProcess.LateUpdate))]
            private static IEnumerable<CodeInstruction> FixBaseProcessLateUpdate(IEnumerable<CodeInstruction> insts)
            {
                //ldarg.0
                //ldarg.0
                //ldloc.0
                //ldloc.2
                //ldloc.3
                //call	instance class [mscorlib]System.Collections.IEnumerator Illusion.Component.Correct.Process.BaseProcess::Restore(class [UnityEngine.CoreModule]UnityEngine.Transform, valuetype ne.CoreModule]//UnityEngine.Vector3, valuetype [UnityEngine.CoreModule]UnityEngine.Quaternion)
                //call	instance class [UnityEngine.CoreModule]UnityEngine.Coroutine [UnityEngine.CoreModule]UnityEngine.MonoBehaviour::StartCoroutine(class [mscorlib]System.Collections.IEnumerator)
                //pop

                return new CodeMatcher(insts)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldloc_0),
                        new CodeMatch(OpCodes.Ldloc_2),
                        new CodeMatch(OpCodes.Ldloc_3),
                        new CodeMatch(OpCodes.Call),
                        new CodeMatch(OpCodes.Call),
                        new CodeMatch(OpCodes.Pop))
                    .ThrowIfInvalid("Could not find StartCoroutine")
                    .SetOpcodeAndAdvance(OpCodes.Nop)
                    .SetOpcodeAndAdvance(OpCodes.Nop)
                    .Advance(3) // Keep the Restore parameters
                    .SetOperandAndAdvance(AccessTools.Method(typeof(AntiGarbageHooks), nameof(QueueBaseProcessUpdateJob)))
                    .SetOpcodeAndAdvance(OpCodes.Nop) // Don't remove StartCoroutine operand in case some other transplier wants to use it as landmark
                    .SetOpcodeAndAdvance(OpCodes.Nop)
                    .Instructions();
            }
            
            private static void QueueBaseProcessUpdateJob(Transform t, Vector3 pos, Quaternion rot)
            {
                _baseProcessUpdateJobStack.Push(new BaseProcessUpdateJob { pos = pos, rot = rot, t = t });
            }

            private struct BaseProcessUpdateJob
            {
                public Transform t;
                public Vector3 pos;
                public Quaternion rot;

                public void Do()
                {
                    if (t == null) return;
                    t.localPosition = pos;
                    t.localRotation = rot;
                }
            }

            private static readonly Stack<BaseProcessUpdateJob> _baseProcessUpdateJobStack = new Stack<BaseProcessUpdateJob>(100);

            public static IEnumerator BaseProcessUpdateJobCo()
            {
                while (true)
                {
                    yield return CoroutineUtils.WaitForEndOfFrame;
                    while (_baseProcessUpdateJobStack.Count > 0)
                        _baseProcessUpdateJobStack.Pop().Do();
                }
            }

            #endregion
        }
    }
}