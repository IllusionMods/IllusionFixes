﻿using BepInEx.Logging;
using UnityEngine;

namespace Common
{
    internal static class Utilities
    {
        public static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource("IllusionFixes");

        public const string ConfigSectionFixes = "Bug Fixes";
        public const string ConfigSectionTweaks = "Tweaks";

        public static bool InsideStudio { get; } = Application.productName == "CharaStudio" || Application.productName == "StudioNEOV2";

        public static bool InsideKoikatsuParty { get; } = Application.productName == "Koikatsu Party";
    }
}
