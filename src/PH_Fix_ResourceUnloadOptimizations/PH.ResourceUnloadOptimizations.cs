using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class ResourceUnloadOptimizations : BaseUnityPlugin
    {
        public const string GUID = "PH_Fix_ResourceUnloadOptimizations";
    }
}
