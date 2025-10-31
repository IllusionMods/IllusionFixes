using System;
using BepInEx.Preloader.Core.Patching;
using Common;

namespace IllusionFixes.Patchers
{
    // Works under BepInEx 6 pre1
    [PatcherPluginInfo("CultureFix", "CultureFix", Constants.PluginsVersion)]
    public class CultureFixPatcher : BasePatcher
    {
        public override void Initialize()
        {
            var cultureCode = Config.Bind(Utilities.ConfigSectionFixes, "Override culture", "ja-JP", "If not empty, set the process culture to this. Works similarly to a locale emulator. Fixes game crashes and lockups on some system locales.\nThe value has to be in the language-region format (e.g. en-US).").Value;

            if (string.IsNullOrEmpty(cultureCode))
            {
                Log.LogInfo("CultureFix is disabled");
                return;
            }

            try
            {
                var culture = System.Globalization.CultureInfo.GetCultureInfo(cultureCode);
                if (culture.IsNeutralCulture)
                {
                    Log.LogWarning((object)$"CultureFix failed to load - The sepecified culture {cultureCode} is neutral. It has to be in the language-region format (e.g. en-US).");
                    return;
                }

                Log.LogInfo((object)$"CultureFix - Forcing process culture to {cultureCode}.");

                System.Globalization.CultureInfo.CurrentCulture = culture;
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

                // Below is needed under IL2CPP to affect game code for a reason I don't know.
                // It's not enough by itself, above is needed as well or plugins will still run under wrong locale.
                var cultureIl = Il2CppSystem.Globalization.CultureInfo.GetCultureInfo(cultureCode);
                Il2CppSystem.Globalization.CultureInfo.CurrentCulture = cultureIl;
                Il2CppSystem.Globalization.CultureInfo.s_DefaultThreadCurrentCulture = cultureIl;
                Il2CppSystem.Globalization.CultureInfo.s_DefaultThreadCurrentUICulture = cultureIl;
            }
            catch (Exception ex)
            {
                Log.LogError((object)$"CultureFix failed to load - Crashed while trying to set culture {cultureCode} - {ex}");
            }
        }
    }
}
