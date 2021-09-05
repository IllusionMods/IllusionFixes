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
                hi.Patch(movenext, transpiler: new HarmonyMethod(typeof(Hooks), nameof(Hooks.HSceneShowerFix)));
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

            internal static IEnumerable<CodeInstruction> HSceneShowerFix(IEnumerable<CodeInstruction> instructions)
            {
                // Find the part that checks if it's inside of a shower (id 15) and make sure it never returns true
                // (the shower code branch turns off ALL accessories, while by default only Sub accessories are turned off, which is much better for acc hair)
                return new CodeMatcher(instructions)
                    .Advance(2982)
                    .MatchForward(true,
                        new CodeMatch(OpCodes.Ldfld),
                        new CodeMatch(OpCodes.Callvirt),
                        new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)15))
                    .ThrowIfInvalid("Could not find map id")
                    .SetOperandAndAdvance(int.MinValue)
                    .Instructions();
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