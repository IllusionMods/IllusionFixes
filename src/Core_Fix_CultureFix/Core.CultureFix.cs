using Common;
using System.Globalization;

namespace IllusionFixes
{
    public partial class CultureFix
    {
        public const string PluginName = "Culture Fix";

        internal void Awake()
        {
            if (!Utilities.FixesConfig.Wrap(Utilities.ConfigSectionFixes, "Fix process culture",
                "Set process culture to ja-JP, similarly to a locale emulator. Fixes game crashes and lockups on some system locales.", true).Value)
                return;

            var culture = CultureInfo.GetCultureInfo("ja-JP");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
    }
}
