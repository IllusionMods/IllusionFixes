using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using Studio;

namespace IllusionFixes
{
    public partial class StudioSceneFileFilter
    {
        public const string PluginName = "Invalid Studio Scene Filter";

        // todo detect if its a kk or ai scene
        private const string StudioToken = "【KStudio】";
        private static readonly byte[] StudioTokenBytes = Encoding.UTF8.GetBytes(StudioToken);

        private static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(StudioSceneFileFilter));
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

            using (var f = File.OpenRead(path))
            {
                PngFile.SkipPng(f);

                if (!TryReadUntilSequence(f, StudioTokenBytes))
                {
                    Logger.Log(BepInEx.Logging.LogLevel.Message | BepInEx.Logging.LogLevel.Warning, "This is not a valid studio scene file - " + Path.GetFileName(path));
                    return false;
                }
            }

            return true;
        }

        private static bool TryReadUntilSequence(Stream stream, byte[] sequence)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (sequence.Length == 0)
                return false;

            while (true)
            {
                var matched = false;
                for (var i = 0; i < sequence.Length; i++)
                {
                    var value = stream.ReadByte();
                    if (value == -1) return false;
                    matched = value == sequence[i];
                    if (matched) continue;
                    stream.Position -= i;
                    break;
                }
                if (matched)
                    return true;
            }
        }
    }
}