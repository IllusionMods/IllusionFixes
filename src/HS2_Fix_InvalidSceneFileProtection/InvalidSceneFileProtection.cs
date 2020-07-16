using BepInEx;
using Common;
using System.Text;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class InvalidSceneFileProtection : BaseUnityPlugin
    {
        public const string GUID = "HS2_Fix_InvalidSceneFileProtection";

        private static readonly byte[][] ValidStudioTokens = { Encoding.UTF8.GetBytes("【StudioNEOV2】"), Encoding.UTF8.GetBytes("【KStudio】") };
    }
}
