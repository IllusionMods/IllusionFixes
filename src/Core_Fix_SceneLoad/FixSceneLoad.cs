using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class FixSceneLoad : BaseUnityPlugin
    {
        public const string GUID = "Fix_SceneLoad";
        public const string PluginName = "Fix SceneLoad";

        internal void Main()
        {
#if KK || KKS
            PageKeeper.Setup();
#endif
        }
    }


        }
