using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace IllusionFixes
{
    public partial class CardImportFix
    {
        private static int SafeGetKind(ListInfoBase instance) => instance == null ? -9999 : instance.Kind;

        internal static class Hooks
        {
            /// <summary>
            /// Prevent items with sideloader-assigned IDs from being removed
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.CheckDataRange))]
            internal static bool CheckDataRangePrefix(ref bool __result)
            {
                __result = true;
                return false;
            }

            /// <summary>
            /// Prevent items with sideloader-assigned IDs from being removed
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.CheckDataRangeCoordinate), typeof(ChaFileCoordinate), typeof(int), typeof(List<string>))]
            internal static bool CheckDataRangeCoordinatePrefix(ref bool __result)
            {
                __result = true;
                return false;
            }

            /// <summary>
            /// Fix null exception when importing characters with modded clothes under some conditions
            /// </summary>
            [HarmonyTranspiler, HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.CheckUsedPackageCoordinate), typeof(ChaFileCoordinate), typeof(HashSet<int>))]
            internal static IEnumerable<CodeInstruction> ImportNullFixTpl(IEnumerable<CodeInstruction> instructions)
            {
                var target = AccessTools.Property(typeof(ListInfoBase), nameof(ListInfoBase.Kind)).GetMethod;
                var replacement = AccessTools.Method(typeof(CardImportFix), nameof(SafeGetKind));

                foreach (var instruction in instructions)
                {
                    if (Equals(instruction.operand, target))
                        yield return new CodeInstruction(OpCodes.Call, replacement);
                    else
                        yield return instruction;
                }
            }
        }
    }
}
