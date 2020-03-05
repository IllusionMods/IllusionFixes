using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class NodeEditorUnlock : BaseUnityPlugin
    {
        public const string GUID = "EC_Fix_NodeEditorUnlock";
        public const string PluginName = "Node Editor Unlock";

        internal void Start()
        {
            if (!Config.Bind(Utilities.ConfigSectionTweaks, "Unlock node limit in scenes", true,
                new ConfigDescription("Unlock the limit of 50 nodes in a single scene file and allow an unlimited amount of nodes.")).Value)
                return;

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
