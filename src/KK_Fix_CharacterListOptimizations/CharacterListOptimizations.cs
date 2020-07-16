using BepInEx;
using BepInEx.Harmony;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)] //Not currently compatible with Koikatsu Party
    [BepInProcess(Constants.VRProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class CharacterListOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_CharacterListOptimizations";
        public const string PluginName = "Character List Optimizations";

        internal void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
