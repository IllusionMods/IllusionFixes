using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using Common;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class NodeEditorUnlock : BaseUnityPlugin
    {
        public const string GUID = "EC_Fix_NodeEditorUnlock";
        public const string PluginName = "Node Editor Unlock";

        internal void Start()
        {
            if (!Utilities.FixesConfig.AddSetting(Utilities.ConfigSectionTweaks, "Unlock node limit in scenes", true,
                new ConfigDescription("Unlock the limit of 50 nodes in a single scene file and allow unlimited amount nodes.")).Value)
                return;

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
