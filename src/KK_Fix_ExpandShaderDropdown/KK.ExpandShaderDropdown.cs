using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class ExpandShaderDropdown : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ExpandShaderDropdown";
    }
}
