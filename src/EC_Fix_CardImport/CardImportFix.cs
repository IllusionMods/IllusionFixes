using BepInEx;
using BepInEx.Configuration;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class CardImportFix : BaseUnityPlugin
    {
        public const string GUID = "EC_Fix_CardImport";
        public const string PluginName = "Card Import Fixes";

        internal void Start()
        {
            if (!Config.Bind(Utilities.ConfigSectionFixes, "Disable card import checks", true,
                new ConfigDescription("Prevents the game from crashing or stripping some modded data when importing KK cards.")).Value)
                return;

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }
}
