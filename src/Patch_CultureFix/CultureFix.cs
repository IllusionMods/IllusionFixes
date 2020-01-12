using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using Common;
using Mono.Cecil;

namespace IllusionFixes.Patchers
{
    public static class CultureFix
    {
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "UnityEngine.dll" };

        public static void Patch(AssemblyDefinition ad)
        {
            var cf = new ConfigFile(Utility.CombinePaths(Paths.ConfigPath, "CultureFix.cfg"), true);

            var cultureCode = cf.Bind(Utilities.ConfigSectionFixes, "Override culture", "ja-JP", "If not empty, set the process culture to this. Works similarly to a locale emulator. Fixes game crashes and lockups on some system locales.\nThe value has to be in the language-region format (e.g. en-US).").Value;

            if (string.IsNullOrEmpty(cultureCode))
            {
                Console.WriteLine("CultureFix is disabled");
                return;
            }

            try
            {
                var culture = CultureInfo.GetCultureInfo("ja-JP");

                if (culture.IsNeutralCulture)
                {
                    Console.WriteLine($"CultureFix failed to load - The sepecified culture {cultureCode} is neutral. It has to be in the language-region format (e.g. en-US).");
                    return;
                }

                Console.WriteLine($"CultureFix - Forcing process culture to {cultureCode}.");

                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CultureFix failed to load - Crashed while trying to set culture {cultureCode} - {ex}");
            }
        }
    }
}
