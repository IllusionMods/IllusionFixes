using System.Globalization;
using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class CultureFix : BaseUnityPlugin
    {
        public const string GUID = "EC_Fix_CultureFix";
        public const string PluginName = "Culture Fix";

        private void Awake()
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
