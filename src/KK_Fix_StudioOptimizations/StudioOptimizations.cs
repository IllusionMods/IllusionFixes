using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class StudioOptimizations : BaseUnityPlugin
    {
    }
}
