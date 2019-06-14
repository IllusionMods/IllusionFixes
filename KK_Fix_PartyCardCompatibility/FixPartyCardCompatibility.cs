using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using Common;
using Harmony;

namespace KK_Fix_PartyCardCompatibility
{
    [BepInPlugin(Guid, Guid, Metadata.PluginsVersion)]
    public class FixPartyCardCompatibility : BaseUnityPlugin
    {
        public const string Guid = "KK_Fix_PartyCardCompatibility";

        private void Awake()
        {
            HarmonyInstance.Create(Guid).PatchAll(typeof(FixPartyCardCompatibility));
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
                        Logger.Log(LogLevel.Error, $"[{Guid}] Failed to hook ChaFile.LoadFile, unexpected IL opcode");

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
