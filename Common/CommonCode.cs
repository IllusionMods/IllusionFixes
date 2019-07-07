using BepInEx.Logging;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace Common
{
    internal class CommonCode
    {
        internal static bool InsideStudio => Application.productName == "CharaStudio";
        internal static bool InsideKoikatsuParty => Application.productName == "Koikatsu Party";
        internal static void Log(string text) => Logger.Log(LogLevel.Info, text);
        internal static void Log(LogLevel level, string text) => Logger.Log(level, text);
        internal static void Log(object text) => Logger.Log(LogLevel.Info, text?.ToString());
        internal static void Log(LogLevel level, object text) => Logger.Log(level, text?.ToString());
    }
}
