using BepInEx;
using BepInEx.Harmony;
using Common;

namespace IllusionFixes
{
    [BepInProcess("CharaStudio")]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class UnlimitedMapLights : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_UnlimitedMapLights";
        public const string PluginName = "Unlimited Map Lights";

        internal void Start() => HarmonyWrapper.PatchAll(typeof(Hooks));
    }
}
