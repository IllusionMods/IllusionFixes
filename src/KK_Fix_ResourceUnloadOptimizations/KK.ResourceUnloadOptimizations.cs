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

                var l1 = generator.DefineLabel();
                var l2 = generator.DefineLabel();

                var replacementInstr = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ), //8
                    new CodeInstruction(OpCodes.Ldfld    , topF                                                   ), //9
                    new CodeInstruction(OpCodes.Brtrue_S , l1                                                     ), //10
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ), //11
                    new CodeInstruction(OpCodes.Newobj   , AccessTools.Constructor(lgT, new Type[0])              ), //12 -> 14
                    new CodeInstruction(OpCodes.Stfld    , topF                                                   ), //13
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ){ labels = new List<Label>{l1}}, //14
                    new CodeInstruction(OpCodes.Ldfld    , winF                                                   ), //15
                    new CodeInstruction(OpCodes.Brtrue_S , l2                                                     ), //16 -> 20
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ), //17
                    new CodeInstruction(OpCodes.Newobj   , AccessTools.Constructor(lgT, new Type[0])              ), //18
                    new CodeInstruction(OpCodes.Stfld    , winF                                                   ), //19
                    new CodeInstruction(OpCodes.Ldsfld   , curF                                                   ){ labels = new List<Label>{l2}}, //20
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ), //21
                    new CodeInstruction(OpCodes.Ldfld    , topF                                                   ), //22
                    new CodeInstruction(OpCodes.Stfld    , topF                                                   ), //23
                    new CodeInstruction(OpCodes.Ldsfld   , curF                                                   ), //24
                    new CodeInstruction(OpCodes.Ldfld    , lgF                                                    ), //25
                    new CodeInstruction(OpCodes.Callvirt , AccessTools.Method(typeof(Stack), nameof(Stack.Clear)) ), //26
                    new CodeInstruction(OpCodes.Ldsfld   , curF                                                   ), //27
                    new CodeInstruction(OpCodes.Ldfld    , lgF                                                    ), //28
                    new CodeInstruction(OpCodes.Ldsfld   , curF                                                   ), //29
                    new CodeInstruction(OpCodes.Ldfld    , topF                                                   ), //30
                    new CodeInstruction(OpCodes.Callvirt ,AccessTools.Method(typeof(Stack), nameof(Stack.Push))   ), //31
                    new CodeInstruction(OpCodes.Ldsfld   , curF                                                   ), //32
                    new CodeInstruction(OpCodes.Ldloc_0                                                           ), //33
                    new CodeInstruction(OpCodes.Ldfld    , winF                                                   ), //34
                    new CodeInstruction(OpCodes.Stfld    , winF                                                   ), //35
                    new CodeInstruction(OpCodes.Ret                                                               ), //36
                };

                var instr = instructions.ToList();

                var start = instr.FindIndex(x => x.opcode == OpCodes.Bne_Un);
                if (start < 0) throw new MissingMemberException("OpCodes.Bne_Un");
                var end = instr.FindIndex(x => x.opcode == OpCodes.Br);
                if (end < 0) throw new MissingMemberException("OpCodes.Br");

                instr.RemoveRange(start + 1, end - start);
                instr.InsertRange(start + 1, replacementInstr);

                return instr;
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
