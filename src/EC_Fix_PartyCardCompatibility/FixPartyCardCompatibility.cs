using Common;
using System;
using BepInEx;
using System.IO;
using HarmonyLib;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class FixPartyCardCompatibility : BaseUnityPlugin
    {
        public const string GUID = "EC_Fix_PartyCardCompatibility";
        public const string PluginName = "Party Card Compatibility";

        internal void Awake() => Harmony.CreateAndPatchAll(typeof(FixPartyCardCompatibility));

        /// <summary>
        /// Needs to return false if the condition passes, true if it fails (this replaces the string != operand)
        /// </summary>
        private static bool CustomCardTokenCompare(string read, string expected)
        {
            if (read == null) return true;

            // Other card types add letters to the end of the string before closing bracket
            var trimmedExpected = expected.Substring(0, expected.Length - 1);
            return !read.StartsWith(trimmedExpected, StringComparison.OrdinalIgnoreCase);
        }

        // Trying to extract this into a Hooks subclass breaks it
        [HarmonyTranspiler, HarmonyPatch(typeof(KoikatsuCharaFile.ChaFile), "LoadFile", new[] { typeof(BinaryReader), typeof(bool), typeof(bool) })]
        public static IEnumerable<CodeInstruction> ChaFileLoadFileTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var patchNext = false;
            foreach (var instruction in instructions)
            {
                if (patchNext)
                {
                    if (instruction.opcode == OpCodes.Call)
                        instruction.operand = AccessTools.Method(typeof(FixPartyCardCompatibility), nameof(CustomCardTokenCompare));
                    else
                        Utilities.Logger.LogError($"[{GUID}] Failed to hook ChaFile.LoadFile, unexpected IL opcode");

                    patchNext = false;
                }
                else if (instruction.operand is string s && s == "【KoiKatuChara】")
                {
                    patchNext = true;
                }

                yield return instruction;
            }
        }
    }
}
