using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ActionGame;
using ActionGame.Chara;
using BepInEx;
using Common;
using HarmonyLib;
using Mono.Cecil;
using MonoMod.Cil;
using MonoMod.Utils;

namespace IllusionFixes
{
    // Code is in the shared project part
    [BepInProcess(Constants.GameProcessName)]
    public partial class ShowerAccessories : BaseUnityPlugin
    {
        private static class Hooks
        {
            public static void ApplyHooks(string guid)
            {
                var hi = Harmony.CreateAndPatchAll(typeof(Hooks), guid);


                var movenext = GetMoveNext(AccessTools.Method(typeof(HSceneProc), nameof(HSceneProc.Start)));
                hi.Patch(movenext, transpiler: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ShowerFixTpl)));
            }

            private static MethodInfo GetMoveNext(MethodInfo targetMethod)
            {
                var ctx = new ILContext(new DynamicMethodDefinition(targetMethod).Definition);
                var il = new ILCursor(ctx);
                MethodReference enumeratorCtor = null;
                il.GotoNext(instruction => instruction.MatchNewobj(out enumeratorCtor));
                if (enumeratorCtor == null) throw new ArgumentNullException(nameof(enumeratorCtor));
                if (enumeratorCtor.Name != ".ctor")
                    throw new ArgumentException($"Unexpected method name {enumeratorCtor.Name}, should be .ctor",
                        nameof(enumeratorCtor));

                var enumeratorType = enumeratorCtor.DeclaringType.ResolveReflection();
                var movenext = enumeratorType.GetMethod("MoveNext", AccessTools.all);
                if (movenext == null) throw new ArgumentNullException(nameof(movenext));
                return movenext;
            }

            internal static IEnumerable<CodeInstruction> ShowerFixTpl(IEnumerable<CodeInstruction> instructions)
            {
                // Find the last fade out that goes into the scene and fix acc state right before it
                return new CodeMatcher(instructions)
                    .Advance(3490) // Reduce unnecessary checks, still has a safety buffer of a few instructions for future updates
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldc_I4_2),
                        new CodeMatch(OpCodes.Ldc_I4_0),
                        new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(FadeCanvas), nameof(FadeCanvas.StartFadeAysnc))))
                    .ThrowIfInvalid("Could " + nameof(FadeCanvas.StartFadeAysnc))
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldloc_1),
                        CodeInstruction.Call(typeof(Hooks), nameof(Hooks.ShowerFixHook)))
                    .Instructions();
            }

            private static void ShowerFixHook(HSceneProc __instance)
            {
                try
                {

                    var map = __instance.map;
                    if (map.no == 15) //shower
                    {
                        var lstFemale = __instance.lstFemale;
                        FixAccessoryState(lstFemale[0]);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(AI), nameof(AI.ArrivalSet))]
            internal static void OverworldShowerFix(AI __instance, ActionControl.ResultInfo result)
            {
                try
                {
                    if (result.actionNo == 2) //todo need to check if same ID in kks
                    {
                        var chaControl = __instance.npc.chaCtrl;
                        FixAccessoryState(chaControl);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }
    }
}