using BepInEx.Harmony;

namespace IllusionFixes
{
    public partial class ManifestCorrector
    {
        public const string PluginName = "Manifest Corrector";

        internal void Start() => HarmonyWrapper.PatchAll(typeof(Hooks));
    }
}