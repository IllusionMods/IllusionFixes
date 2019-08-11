using BepInEx;
using BepInEx.Harmony;
using Common;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class CardImportFix : BaseUnityPlugin
    {
        public const string GUID = "EC_Fix_CardImport";
        public const string PluginName = "Card Import Fixes";
        public const string Version = Metadata.PluginsVersion;

        private void Start()
        {
            if (!Utilities.FixesConfig.Wrap(Utilities.ConfigSectionFixes, "Disable card import checks",
                "Prevents the game from crashing or stripping some modded data when importing KK cards.", true).Value)
                return;

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
