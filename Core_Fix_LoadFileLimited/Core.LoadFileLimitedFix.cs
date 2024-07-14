using BepInEx;
using HarmonyLib;
using Common;

namespace Core_Fix_LoadFileLimited
{
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class LoadFileLimitedFix : BaseUnityPlugin
    {
        public const string GUID = "LoadFileLmitedFix";
        public const string PluginName = "LoadFileLimitedFix";

        internal void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }
}