using System.Text;
using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class InvalidSceneFileProtection : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_InvalidSceneFileProtection";

        private static readonly byte[][] ValidStudioTokens = { Encoding.UTF8.GetBytes("【KStudio】") };
    }
}
