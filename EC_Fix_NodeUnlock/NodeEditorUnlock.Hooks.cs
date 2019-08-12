using HarmonyLib;
using HEdit;
using UniRx;
using YS_Node;

namespace IllusionFixes
{
    public partial class NodeEditorUnlock
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(NodeSettingCanvas), nameof(NodeSettingCanvas.NodeRestriction))]
            public static bool NodeRestrictionPrefix() => false;

            [HarmonyPostfix, HarmonyPatch(typeof(NodeUI), "Start")]
            public static void NodeUIStartPostfix(NodeUI __instance) => Traverse.Create(__instance).Field("limitOver").GetValue<BoolReactiveProperty>().Dispose();
        }
    }
}
