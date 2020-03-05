using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class ModdedHeadEyelinerFix : BaseUnityPlugin
    {
        public const string GUID = "EC_Fix_ModdedHeadEyeliner";
    }
}
