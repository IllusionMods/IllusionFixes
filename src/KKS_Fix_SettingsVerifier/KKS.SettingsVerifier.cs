using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class SettingsVerifier : BaseUnityPlugin
    {
        public const string GUID = "KKS_Fix_SettingsVerifier";
    }
}
