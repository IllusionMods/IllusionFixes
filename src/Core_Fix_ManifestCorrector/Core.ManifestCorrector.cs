using HarmonyLib;

namespace IllusionFixes
{
    public partial class ManifestCorrector
    {
        public const string PluginName = "Manifest Corrector";

        internal void Start() => Harmony.CreateAndPatchAll(typeof(Hooks));
    }
}