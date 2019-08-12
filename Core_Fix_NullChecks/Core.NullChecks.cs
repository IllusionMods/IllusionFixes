using BepInEx.Harmony;
using Common;

namespace IllusionFixes
{
    public partial class NullChecks
    {
        public const string GUID = "com.deathweasel.bepinex.cutscenelockupfix";
        public const string PluginName = "Null Checks";

        public void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
