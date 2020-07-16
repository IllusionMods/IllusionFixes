using BepInEx;
using Common;

namespace IllusionFixes
{
    /// <summary>
    /// Corrects Honey Select poses loaded in HoneySelect2 and prevents error spam
    /// </summary>
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class PoseLoad : BaseUnityPlugin
    {
        public const string GUID = "HS2_Fix_PoseLoad";
    }
}
