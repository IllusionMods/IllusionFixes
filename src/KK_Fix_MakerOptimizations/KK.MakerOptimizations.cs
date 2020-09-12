using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class MakerOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_MakerOptimizations";

        private void Start()
        {
            var virtualize = Config.Bind(Utilities.ConfigSectionTweaks, "Virtualize maker lists", true, "Major load time reduction and performance improvement in character maker. Eliminates lag when switching tabs." +
                                                                                                                 "\nCan cause some compatibility issues with other plugins." +
                                                                                                                 "\nChanges take effect after game restart.");
            if (virtualize.Value) VirtualizeMakerLists.InstallHooks();
        }
    }
}
