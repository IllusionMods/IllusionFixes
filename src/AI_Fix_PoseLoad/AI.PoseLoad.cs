using BepInEx;
using Common;

namespace IllusionFixes
{
    /// <summary>
    /// Corrects Honey Select poses loaded in AI Girl and prevents error spam
    /// </summary>
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class PoseLoad : BaseUnityPlugin
    {
        public const string GUID = "AI_Fix_PoseLoad";
    }
}
