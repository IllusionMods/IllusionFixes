using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class SettingsVerifier : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_SettingsVerifier";
        private static SettingsVerifier _instance;

        private SettingsVerifier() => _instance = this;
    }
}
