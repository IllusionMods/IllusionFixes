using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class HeterochromiaFix : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_Heterochromia";
    }
}
