using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class MakerMaleFaceTypes : BaseUnityPlugin
    {
        public const string GUID = "HS2_Fix_MakerMaleFaceTypes";
    }
}
