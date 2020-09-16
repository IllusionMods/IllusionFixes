using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using Common;
using HarmonyLib;
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
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(GUILayoutUtility), "Begin")]
            private static IEnumerable<CodeInstruction> FixOnguiGarbageDump(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                var curP = AccessTools.PropertyGetter(typeof(UnityEngine.Event), "current");
                var typeP = AccessTools.PropertyGetter(typeof(UnityEngine.Event), "type");

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
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ),
                    new CodeInstruction(OpCodes.Ldfld    , topF                                                   ),
                    new CodeInstruction(OpCodes.Brtrue_S , l1                                                     ),
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ),
                    new CodeInstruction(OpCodes.Newobj   , AccessTools.Constructor(lgT, new Type[0])              ),
                    new CodeInstruction(OpCodes.Stfld    , topF                                                   ),
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ){ labels = new List<Label>{l1}}, 
                    new CodeInstruction(OpCodes.Ldfld	 , topF                                                   ),
                    new CodeInstruction(OpCodes.Ldfld	 , entrF                                                  ),
                    new CodeInstruction(OpCodes.Callvirt , entrClearM                                             ),
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ),
                    new CodeInstruction(OpCodes.Ldfld    , winF                                                   ),
                    new CodeInstruction(OpCodes.Brtrue_S , l2                                                     ),
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ),
                    new CodeInstruction(OpCodes.Newobj   , AccessTools.Constructor(lgT, new Type[0])              ),
                    new CodeInstruction(OpCodes.Stfld    , winF                                                   ),
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ){ labels = new List<Label>{l2}},
                    new CodeInstruction(OpCodes.Ldfld	 , winF                                                   ),
                    new CodeInstruction(OpCodes.Ldfld	 , entrF                                                  ),
                    new CodeInstruction(OpCodes.Callvirt , entrClearM                                             ),
                    new CodeInstruction(OpCodes.Ldsfld   , curF                                                   ),
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ),
                    new CodeInstruction(OpCodes.Ldfld    , topF                                                   ),
                    new CodeInstruction(OpCodes.Stfld    , topF                                                   ),
                    new CodeInstruction(OpCodes.Ldsfld   , curF                                                   ),
                    new CodeInstruction(OpCodes.Ldfld    , lgF                                                    ),
                    new CodeInstruction(OpCodes.Callvirt , AccessTools.Method(typeof(Stack), nameof(Stack.Clear)) ),
                    new CodeInstruction(OpCodes.Ldsfld   , curF                                                   ),
                    new CodeInstruction(OpCodes.Ldfld    , lgF                                                    ),
                    new CodeInstruction(OpCodes.Ldsfld   , curF                                                   ),
                    new CodeInstruction(OpCodes.Ldfld    , topF                                                   ),
                    new CodeInstruction(OpCodes.Callvirt ,AccessTools.Method(typeof(Stack), nameof(Stack.Push))   ),
                    new CodeInstruction(OpCodes.Ret                                                               ),
                    new CodeInstruction(OpCodes.Ldsfld, curF){ labels = new List<Label>{l0}}
                };

                var instr = instructions.ToList();
                var c = 0;
                for (int i = instr.Count - 1; i >= 0; i--)
                {
                    c = c + Convert.ToInt32(instr[i].opcode == OpCodes.Ldsfld);
                    if (c == 3)
                    {
                        for (int j = i + 1; j < instr.Count; j++)
                        {
                            replacementInstr.Add(instr[j]);
                        }
                        break;
                    }
                }

                if (c != 3) throw new InvalidOperationException("IL footprint does not match expected?");
                UnityEngine.Debug.Log("IMGUI Patch done");
                return replacementInstr;
            }

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
