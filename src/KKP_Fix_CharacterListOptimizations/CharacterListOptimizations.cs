using System.Linq;
using BepInEx;
using Common;
using ExtensibleSaveFormat;
using HarmonyLib;
using Localize.Translate;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class CharacterListOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KKP_Fix_CharacterListOptimizations";
        public const string PluginName = "Character List Optimizations";

        internal void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        // In KKP and KKP_VR pretty much all lists use CustomFileListSelecter so the same patches work for everything
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
                    var files = __instance.files;
                    var fileInfo = files.First(x => x.chaFile == control);
                    control.LoadCharaFile(fileInfo.info.FullPath);
                };
            }
        }
    }
}
