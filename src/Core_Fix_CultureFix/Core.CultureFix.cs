using BepInEx.Configuration;
using Common;
using System.Globalization;

namespace IllusionFixes
{
    public partial class CultureFix
    {
        public const string PluginName = "Culture Fix";

        internal void Awake()
        {
            if (!Config.AddSetting(Utilities.ConfigSectionFixes, "Fix process culture", true,
                new ConfigDescription("Set process culture to ja-JP, similarly to a locale emulator. Fixes game crashes and lockups on some system locales.")).Value)
                return;

            var culture = CultureInfo.GetCultureInfo("ja-JP");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}
