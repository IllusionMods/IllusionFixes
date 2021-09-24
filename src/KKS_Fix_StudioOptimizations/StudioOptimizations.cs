using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class StudioOptimizations : BaseUnityPlugin
    {
    }
}
