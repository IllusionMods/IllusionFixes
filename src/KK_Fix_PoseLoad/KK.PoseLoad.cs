using BepInEx;
using Common;

namespace IllusionFixes
{
    /// <summary>
    /// Corrects Honey Select poses loaded in Koikatsu and prevents error spam
    /// </summary>
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class PoseLoad : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_PoseLoad";
    }
}
