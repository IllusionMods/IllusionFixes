using BepInEx;
using BepInEx.Harmony;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class NewGameShowAllCardsFix : BaseUnityPlugin
    {
        public const string GUID = "AI_Fix_NewGameShowAllCards";
        public const string PluginName = "Show All Cards In New Game List Fix";

        private void Awake()
        {
            HarmonyWrapper.PatchAll(typeof(NewGameShowAllCardsFix));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameLoadCharaFileSystem.GameLoadCharaWindow), "Start")]
        private static void StartPrefix(GameLoadCharaFileSystem.GameLoadCharaWindow __instance)
        {
            // Only run at first startup
            if (__instance.IsStartUp) return;

            __instance.useDownload = true;
        }
    }
}
