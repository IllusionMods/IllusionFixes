using BepInEx;
using BepInEx.Harmony;
using Common;

namespace KK_Fix_ShowerAccessories
{
    /// <summary>
    /// Prevents accessories from being disabled in the shower peeping mode
    /// </summary>
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class ShowerAccessories : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ShowerAccessories";
        public const string PluginName = "Shower Accessories Fix";

        private void Awake() => HarmonyWrapper.PatchAll(typeof(Hooks));
    }
}
