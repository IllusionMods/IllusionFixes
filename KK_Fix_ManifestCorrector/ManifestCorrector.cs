using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using Common;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class ManifestCorrector : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ManifestCorrector";
        public const string PluginName = "Manifest Corrector";

        internal void Start() => HarmonyWrapper.PatchAll(typeof(Hooks));
    }
}