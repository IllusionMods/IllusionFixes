using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class ManifestCorrector : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ManifestCorrector";
    }
}