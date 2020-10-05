using BepInEx;
using Common;
using HarmonyLib;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class CenteredHSceneCursor : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_CenteredHSceneCursor";
        public const string PluginName = "Centered HScene Cursor";

        public void Awake() => Harmony.CreateAndPatchAll(typeof(Hooks));

        internal static class Hooks
        {
            [HarmonyPostfix, HarmonyPatch(typeof(GameCursor), "SetCursorTexture")]
            internal static void CenterCursor(GameCursor __instance, ref int _kind)
            {
                if (_kind == -2)
                {
                    var sizeWindow = Traverse.Create(__instance).Field("sizeWindow").GetValue<int>();
                    var tex = __instance.iconDefalutTextures[sizeWindow];
                    var center = new Vector2(tex.width / 2, tex.height / 2);
                    Cursor.SetCursor(tex, center, CursorMode.ForceSoftware);
                }
            }
        }
    }
}
