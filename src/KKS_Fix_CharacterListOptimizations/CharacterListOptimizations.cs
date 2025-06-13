using BepInEx;
using ChaCustom;
using Common;
using ExtensibleSaveFormat;
using HarmonyLib;
using System.Collections;
using System.Diagnostics;
using BepInEx.Logging;
using Localize.Translate;
using UnityEngine.Events;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class CharacterListOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KKS_Fix_CharacterListOptimizations";
        public const string PluginName = "Character List Optimizations";

        private static new ManualLogSource Logger;

        private Harmony _hi;

        private void Awake()
        {
            Logger = base.Logger;
            _hi = Harmony.CreateAndPatchAll(typeof(Hooks));
        }

#if DEBUG
        private void OnDestroy()
        {
            _hi?.UnpatchSelf();
        }
#endif

        private class Hooks
        {
            /// <summary>
            /// Turn off ExtensibleSaveFormat events as the card list is loaded. Speeds things up substantially for modded cards.
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(CustomFileListSelecter), nameof(CustomFileListSelecter.Initialize))]
            private static void FileListInitializePre(out Stopwatch __state)
            {
                ExtendedSave.LoadEventsEnabled = false;
                __state = Stopwatch.StartNew();
            }

            [HarmonyPostfix, HarmonyPatch(typeof(CustomFileListSelecter), nameof(CustomFileListSelecter.Initialize))]
            private static void FileListInitializePost(Stopwatch __state)
            {
                ExtendedSave.LoadEventsEnabled = true;
                Logger.LogDebug("Character list load took " + __state.ElapsedMilliseconds + "ms");
            }

            /// <summary>
            /// Fix turning off ExtensibleSaveFormat events causing card picked in the card list not having any ExtensibleSaveFormat data on it.
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(CustomFileListSelecter), nameof(CustomFileListSelecter.Start))]
            private static void CustomFileListSelecterHook(CustomFileListSelecter __instance, ref IEnumerator __result)
            {
                __result = __result.AppendCo(Postfix);

                void Postfix()
                {
                    // Keep track of the currently selected card
                    CustomFileInfo currentInfo = null;
                    __instance.listCtrl.eventOnPointerClick += info => currentInfo = info ?? currentInfo;

                    foreach (var button in __instance.enter)
                    {
                        // Add a call to the Accept button that happens before the base game call to grab the card and close the list
                        // This is needed to load extended data of the card, since it is skipped in the patches above
                        button.onClick.m_Calls.m_RuntimeCalls.Insert(0, new InvokableCall(() => __instance.info.Value.LoadCharaFile(currentInfo.FullPath)));
                    }
                }
            }

        }
    }
}
