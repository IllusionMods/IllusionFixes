using System.Text;
using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess("StudioNEOV2")]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class InvalidSceneFileProtection : BaseUnityPlugin
    {
        public const string GUID = "AI_Fix_InvalidSceneFileProtection";

        private static readonly byte[][] ValidStudioTokens = { Encoding.UTF8.GetBytes("【StudioNEOV2】"), Encoding.UTF8.GetBytes("【KStudio】") };
    }
}
