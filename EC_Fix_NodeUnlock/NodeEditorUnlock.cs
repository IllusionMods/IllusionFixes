using BepInEx;
using BepInEx.Harmony;
using Common;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class NodeEditorUnlock : BaseUnityPlugin
    {
        public const string GUID = "EC.Core.Fixes.NodeUnlock";
        public const string PluginName = "Node Editor Unlock";
        public const string Version = Metadata.PluginsVersion;

        private void Start()
        {
            if (!Utilities.FixesConfig.Wrap(Utilities.ConfigSectionTweaks, "Unlock node limit in scenes",
                "Unlock the limit of 50 nodes in a single scene file and allow unlimited amount nodes.", true).Value)
                return;

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
