using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using Common;
using HarmonyLib;
using Illusion.Component.Correct.Process;
using ILSetUtility.TimeUtility;
using Manager;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, GUID, Constants.PluginsVersion)]
    [BepInDependency(Screencap.ScreenshotManager.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class GarbageTruck : BaseUnityPlugin
    {
        public const string GUID = "KKS_Fix_GarbageTruck";

        private void Start()
        {
            Harmony.CreateAndPatchAll(typeof(AntiGarbageHooks));

            StartCoroutine(BaseProcessUpdateJobCo());

            // If screencap is installed, disable updates while a screenshot is being taken to fix issue where with FK enabled and hands posed in a way
            // other than the default and pose, accessories attached to fingers such as rings appear in the position they would be in with FK disabled
            var type = Type.GetType("Screencap.ScreenshotManager, KKS_Screencap");
            if (type != null)
            {
                var preEvent = type.GetEvent(nameof(Screencap.ScreenshotManager.OnPreCapture), BindingFlags.Static | BindingFlags.Public);
                if (preEvent != null) preEvent.AddEventHandler(null, new Action(() => enabled = false));
                var postEvent = type.GetEvent(nameof(Screencap.ScreenshotManager.OnPostCapture), BindingFlags.Static | BindingFlags.Public);
                if (postEvent != null) postEvent.AddEventHandler(null, new Action(() => enabled = true));
            }
        }

        private static readonly WaitForEndOfFrame _cachedWaitForEndOfFrame = new WaitForEndOfFrame();
        private IEnumerator BaseProcessUpdateJobCo()
        {
            while (true)
            {
                yield return _cachedWaitForEndOfFrame;
                // Need to check enabled here because the coroutine will still be run regardless
                if (!enabled) continue;
                AntiGarbageHooks.RunUpdateJobs();
            }
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
                // Remove unnecessary new object creation that gets immediately overwritten
                return new CodeMatcher(instructions, generator)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Newobj),
                        new CodeMatch(OpCodes.Stloc_1),
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld))
                    .ThrowIfInvalid("Couldn't find the new object opcode")
                    .RemoveInstruction()
                    .RemoveInstruction()
                    .Instructions();
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

            #region GetAudioWaveValue caching

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

            #endregion

            #region Fix BaseProcessLateUpdate StartCoroutine allocations

            /// <summary>
            /// Spawning a new coroutine every time is very expensive in cpu and gc
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(BaseProcess), nameof(BaseProcess.LateUpdate))]
            private static IEnumerable<CodeInstruction> FixBaseProcessLateUpdate(IEnumerable<CodeInstruction> insts)
            {
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
                    .Advance(3) // Keep parameters given to the original Restore so we can reuse them
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

            public static void RunUpdateJobs()
            {
                while (_baseProcessUpdateJobStack.Count > 0)
                    _baseProcessUpdateJobStack.Pop().Do();
            }

            #endregion
        }
    }
}