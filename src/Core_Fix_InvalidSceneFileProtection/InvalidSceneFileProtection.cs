using BepInEx.Logging;
using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;

namespace IllusionFixes
{
    public partial class InvalidSceneFileProtection
    {
        public const string PluginName = "Invalid Scene Protection";

        private static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            var hi = Harmony.CreateAndPatchAll(typeof(InvalidSceneFileProtection));
            var tpl = new HarmonyMethod(typeof(InvalidSceneFileProtection), nameof(AddExceptionHandler));
            hi.Patch(AccessTools.Method(typeof(SceneInfo), nameof(SceneInfo.Load), new[] { typeof(string), typeof(Version).MakeByRefType() }), null, null, tpl);
            hi.Patch(AccessTools.Method(typeof(SceneInfo), nameof(SceneInfo.Import), new[] { typeof(string) }), null, null, tpl);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad")]
        private static bool OnClickLoadPrefix(List<string> ___listPath, int ___select)
        {
            var path = ___listPath[___select];
            return IsFileValid(path);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneLoadScene), "OnClickImport")]
        private static bool OnClickImportPrefix(List<string> ___listPath, int ___select)
        {
            var path = ___listPath[___select];
            return IsFileValid(path);
        }

        private static bool IsFileValid(string path)
        {
            if (!File.Exists(path)) return false;

            try
            {
                using (var fs = File.OpenRead(path))
                {
                    PngFile.SkipPng(fs);

                    var pos = fs.Position;
                    foreach (var tokenSequence in ValidStudioTokens)
                    {
                        if (Util.FindPosition(fs, tokenSequence) > 0)
                            return true;

                        fs.Seek(pos, SeekOrigin.Begin);
                    }
                }

                LogInvalid();
                return false;
            }
            catch (Exception e)
            {
                // If the check crashes then don't prevent loading the scene in case it's actually good and this code is not
                Logger.LogError(e);
                return true;
            }
        }

        /// <summary>
        /// Converts the using block into a try catch finally that eats the exception instead of letting it crash upwards
        /// </summary>
        private static IEnumerable<CodeInstruction> AddExceptionHandler(IEnumerable<CodeInstruction> inst, ILGenerator ilGenerator)
        {
            var instructions = inst.ToList();

            var finallyBlockIndex = instructions.FindLastIndex(c => c.blocks.Count == 1 && c.blocks[0].blockType == ExceptionBlockType.BeginFinallyBlock);

            var catchBlock = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InvalidSceneFileProtection), nameof(LogCrash)));
            catchBlock.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(SystemException)));
            instructions.Insert(finallyBlockIndex, catchBlock);
            var endLabel = ilGenerator.DefineLabel();
            instructions.Insert(finallyBlockIndex + 1, new CodeInstruction(OpCodes.Leave, endLabel));

            var loadFalse = new CodeInstruction(OpCodes.Ldc_I4_0);
            loadFalse.labels.Add(endLabel);
            instructions.Add(loadFalse);
            instructions.Add(new CodeInstruction(OpCodes.Ret));

            return instructions;
        }

        private static void LogCrash(Exception ex)
        {
            Logger.Log(BepInEx.Logging.LogLevel.Message | BepInEx.Logging.LogLevel.Warning, "Failed to load the file - This scene is from a different game or it is corrupted");
            Logger.LogDebug(ex);
        }

        private static void LogInvalid()
        {
            Logger.Log(BepInEx.Logging.LogLevel.Message | BepInEx.Logging.LogLevel.Warning, "Cannot load the file - This is not a studio scene, it's a scene from a different game, or it is corrupted");
        }
    }
}