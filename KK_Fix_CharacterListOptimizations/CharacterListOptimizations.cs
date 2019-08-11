using BepInEx;
using BepInEx.Harmony;
using Common;

namespace KK_Fix_CharacterListOptimizations
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class CharacterListOptimizations : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.miscfixes";
        public const string PluginName = "Character List Optimizations";

        private void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            if (!CommonCode.InsideKoikatsuParty)
                HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
