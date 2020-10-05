using BepInEx;
using Common;
using ExtensibleSaveFormat;
using HarmonyLib;
using Localize.Translate;
using System.Linq;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessNameSteam)] //Not currently compatible with Koikatsu Party
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class CharacterListOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_CharacterListOptimizations";
        public const string PluginName = "Character List Optimizations";

        internal void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private class Hooks
        {
            /// <summary>
            /// Turn ExtensibleSaveFormat events off and then back on after the characters get loaded
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomFileListSelecter), "Initialize")]
            internal static void ListInitPrefix() => ExtendedSave.LoadEventsEnabled = false;
            [HarmonyPostfix, HarmonyPatch(typeof(CustomFileListSelecter), "Initialize")]
            internal static void ListInitPostfix() => ExtendedSave.LoadEventsEnabled = true;

            /// <summary>
            /// After choosing a card reload it to trigger the ExtensibleSaveFormat events
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(CustomFileListSelecter), "Awake")]
            internal static void ListAwake(CustomFileListSelecter __instance)
            {
                __instance.onEnter += control =>
                {
                    var files = Traverse.Create(__instance).Field<Localize.Translate.Manager.ChaFileInfo[]>("files").Value;
                    var fileInfo = files.First(x => x.chaFile == control);
                    control.LoadCharaFile(fileInfo.info.FullPath);
                };
            }
        }
    }
}
