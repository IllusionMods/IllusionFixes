using BepInEx.Logging;

namespace Common
{
    public static class Utilities
    {
        public static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource("IllusionFixes");

        public const string ConfigSectionFixes = "Bug Fixes";
        public const string ConfigSectionTweaks = "Tweaks";
    }
}
