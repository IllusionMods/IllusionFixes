using BepInEx;
using BepInEx.Harmony;
using Common;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class NodeEditorUnlock : BaseUnityPlugin
    {
        public const string GUID = "EC_Fix_NodeEditorUnlock";
        public const string PluginName = "Node Editor Unlock";

        private void Start()
        {
            if (!Utilities.FixesConfig.Wrap(Utilities.ConfigSectionTweaks, "Unlock node limit in scenes",
                "Unlock the limit of 50 nodes in a single scene file and allow unlimited amount nodes.", true).Value)
                return;

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
