using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class DynamicBonesFix : BaseUnityPlugin
    {
        public const string GUID = "Fix_DynamicBones";
        public const string PluginName = "Dynamic Bones Fix";

        internal void Main() => Harmony.CreateAndPatchAll(typeof(Hooks));

        internal static class Hooks
        {
            //Disable the SkipUpdateParticles method since it causes problems, namely causing jittering when the FPS is higher than 60
            [HarmonyPrefix, HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.SkipUpdateParticles))]
            internal static bool SkipUpdateParticles() => false;
            [HarmonyPrefix, HarmonyPatch(typeof(DynamicBone_Ver02), nameof(DynamicBone_Ver02.SkipUpdateParticles))]
            internal static bool SkipUpdateParticlesVer02() => false;

#if KK || EC || KKS
            // Fix dynamicbone exclusions feature not working (crashing)
            // This fix is already included in AI and HS2 codebase, but not in KK, EC and KKS
            [HarmonyTranspiler, HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.AppendParticles))]
            internal static IEnumerable<CodeInstruction> FixAppendParticlesExclusionsCrash(IEnumerable<CodeInstruction> instructions)
            {
                // The issue is that the loop increments the wrong index variable (the variable for the outer loop)
                // To fix it: find the loop, find reference of its index variable, replace references to the wrong variable
                var cm = new CodeMatcher(instructions);
                cm.MatchForward(true,
                                new CodeMatch(OpCodes.Ldarg_0),
                                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DynamicBone), nameof(DynamicBone.m_Exclusions))),
                                new CodeMatch(OpCodes.Brfalse),
                                new CodeMatch(OpCodes.Ldc_I4_0),
                                new CodeMatch(OpCodes.Stloc_S))
                  .ThrowIfNotMatch("1", new CodeMatch(OpCodes.Stloc_S), new CodeMatch(OpCodes.Br));
                // Reference to the loop's index variable
                var loopLocal = cm.Operand;
                // Find and replace the offending references to the outer index variable (this is a ++ increment operation)
                cm.MatchForward(false,
                                new CodeMatch(OpCodes.Ldloc_S),
                                new CodeMatch(OpCodes.Ldc_I4_1),
                                new CodeMatch(OpCodes.Add),
                                new CodeMatch(OpCodes.Stloc_S))
                  .ThrowIfFalse("2", matcher => matcher.Opcode == OpCodes.Ldloc_S && matcher.Operand != loopLocal)
                  .SetOperandAndAdvance(loopLocal)
                  .Advance(2)
                  .ThrowIfFalse("3", matcher => matcher.Opcode == OpCodes.Stloc_S && matcher.Operand != loopLocal)
                  .SetOperandAndAdvance(loopLocal);
                // yay, thanks to essu for finding the issue
                return cm.Instructions();
            }
#endif
        }
    }
}