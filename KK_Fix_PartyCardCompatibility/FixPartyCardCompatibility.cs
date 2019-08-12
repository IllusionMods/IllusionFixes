using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using Common;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class FixPartyCardCompatibility : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_PartyCardCompatibility";
        public const string PluginName = "Party Card Compatibility";
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(FixPartyCardCompatibility));
        }

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
        [HarmonyTranspiler, HarmonyPatch(typeof(ChaFile), "LoadFile", new[] { typeof(BinaryReader), typeof(bool), typeof(bool) })]
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
                        Logger.Log(LogLevel.Error, $"[{GUID}] Failed to hook ChaFile.LoadFile, unexpected IL opcode");

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
