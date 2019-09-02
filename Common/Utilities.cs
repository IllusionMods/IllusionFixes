using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.IO;

namespace Common
{
    public static class Utilities
    {
        public static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource("IllusionFixes");
        public static ConfigFile FixesConfig { get; } = new ConfigFile(Path.Combine(Paths.ConfigPath, "IllusionFixes.cfg"), false);

        public const string ConfigSectionFixes = "Bug Fixes";
        public const string ConfigSectionTweaks = "Tweaks";
    }
}
