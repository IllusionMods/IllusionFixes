using BepInEx;
using BepInEx.Harmony;
using Common;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class CharacterListOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_CharacterListOptimizations";
        public const string PluginName = "Character List Optimizations";

        private void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            if (!CommonCode.InsideKoikatsuParty) //Not currently compatible with Koikatsu Party
                HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
