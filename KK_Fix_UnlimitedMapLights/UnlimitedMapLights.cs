using BepInEx;
using BepInEx.Harmony;
using Common;

namespace KK_Fix_UnlimitedMapLights
{
    [BepInProcess("CharaStudio")]
    [BepInPlugin("keelhauled.unlimitedmaplights", "UnlimitedMapLights", Metadata.PluginsVersion)]
    public partial class UnlimitedMapLights : BaseUnityPlugin
    {
        private void Start() => HarmonyWrapper.PatchAll(typeof(UnlimitedMapLights));
    }
}
