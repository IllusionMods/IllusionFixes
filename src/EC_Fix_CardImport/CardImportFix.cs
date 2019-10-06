using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using Common;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class CardImportFix : BaseUnityPlugin
    {
        public const string GUID = "EC_Fix_CardImport";
        public const string PluginName = "Card Import Fixes";

        internal void Start()
        {
            if (!Utilities.FixesConfig.AddSetting(Utilities.ConfigSectionFixes, "Disable card import checks", true,
                new ConfigDescription("Prevents the game from crashing or stripping some modded data when importing KK cards.")).Value)
                return;

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
