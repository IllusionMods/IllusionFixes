using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class LoadFileLimitedFix : BaseUnityPlugin
    {
        public const string GUID = "LoadFileLmitedFix";
        public const string PluginName = "LoadFileLimitedFix";

        internal void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        internal class Hooks
        {
            // Makes ChaFileControl.LoadFileLimited (used by Maker to partially load a character) overwrite the charaFileName property whenever new parameters are loaded.
            // This should keep the value of that property and the character the player thinks of as currently loaded in sync. 
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadFileLimited), typeof(string), typeof(byte), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool)
#if KKS
                    , typeof(bool)
#endif
                )]
            private static IEnumerable<CodeInstruction> MakeLoadLimitedCopyCardFileNameTooWhenCopyingParameters(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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