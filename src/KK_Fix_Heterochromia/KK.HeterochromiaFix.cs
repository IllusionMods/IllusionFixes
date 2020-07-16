using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class HeterochromiaFix : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_Heterochromia";
    }
}
