using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace Core_Fix_LoadFileLimited
{
    public partial class LoadFileLimitedFix
    {
        internal class Hooks
        {
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFileLimited), typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool)
#if KKS
                    ,typeof(bool)
#endif
                )]
            private static IEnumerable<CodeInstruction> LoadFileLimitedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> code = new List<CodeInstruction>(instructions);

                CodeMatcher cm = new CodeMatcher(instructions, generator);

                // match [this.parameter.Copy(chaFileControl.parameter);]
                cm.MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ChaFileParameter), nameof(ChaFileParameter.Copy))));
                // insert [base.charaFileName = chaFileControl.charaFileName;]
                cm.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldloc_1),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ChaFile), nameof(ChaFile.charaFileName))),
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertySetter(typeof(ChaFile), nameof(ChaFile.charaFileName)))
                    );

                return cm.Instructions();
            }
        }
    }
}
