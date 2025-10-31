using HarmonyLib;
using HEdit;
using YS_Node;

namespace IllusionFixes
{
    public partial class NodeEditorUnlock
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(NodeSettingCanvas), nameof(NodeSettingCanvas.NodeRestriction))]
            internal static bool NodeRestrictionPrefix() => false;

            [HarmonyPostfix, HarmonyPatch(typeof(NodeUI), "Start")]
            internal static void NodeUIStartPostfix(NodeUI __instance) => __instance.limitOver.Dispose();
        }
    }
}
