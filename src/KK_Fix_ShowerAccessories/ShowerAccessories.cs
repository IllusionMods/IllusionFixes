using BepInEx;
using BepInEx.Harmony;
using Common;

namespace IllusionFixes
{
    /// <summary>
    /// Prevents accessories from being disabled in the shower peeping mode
    /// </summary>
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class ShowerAccessories : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ShowerAccessories";
        public const string PluginName = "Shower Accessories Fix";

        internal void Awake() => HarmonyWrapper.PatchAll(typeof(Hooks));
    }
}
