using System;
using System.IO;
using System.Reflection;
using ADV;
using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using ChaCustom;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class DataCorruptionFixes : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_LoadingFixes";
        public const string PluginName = "Game Data Corruption Fixes";

        private static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            var h = HarmonyWrapper.PatchAll(typeof(DataCorruptionFixes));

            // Only exists in KKParty
            var td = Type.GetType("TutorialData, Assembly-CSharp", false);
            if (td != null)
                h.Patch(td.GetMethod("Load", AccessTools.all), finalizer: new HarmonyMethod(typeof(DataCorruptionFixes), nameof(CatchCrash)));
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(GlobalSaveData), nameof(GlobalSaveData.Load))]
        [HarmonyPatch(typeof(RankingData), nameof(RankingData.Load))]
        [HarmonyPatch(typeof(WeddingData), nameof(WeddingData.Load))]
        [HarmonyPatch(typeof(MemoriesData), nameof(MemoriesData.Load))]
        [HarmonyPatch(typeof(DownloadScene), nameof(DownloadScene.LoadCacheSetting))] // uploader card caching setting
        [HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.LoadItemID))] // checked items
        [HarmonyPatch(typeof(CustomBase.CustomSettingSave), nameof(CustomBase.CustomSettingSave.Load))] // maker config
        private static Exception CatchCrash(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null)
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, $"Corrupted save file detected in {__originalMethod.DeclaringType?.Name}, some progress might be lost.");
                Logger.LogError(__exception);
            }

            return null;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(MainScenario), nameof(MainScenario.LoadReadInfo))] // already read adv text
        private static Exception CatchReadInfoCrash(Exception __exception)
        {
            if (__exception != null)
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "ADV memory file was corrupted, you might no longer be able to skip previously read text.");
                Logger.LogError(__exception);
                
                File.Delete(Path.Combine(UserData.Path, "save/read.dat"));
                // Try again, this time it'll create a blank state since file doesn't exist
                MainScenario.LoadReadInfo();
            }

            return null;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(CommonLib), nameof(CommonLib.GetUUID))] // user id for managing cards on uploader
        private static Exception CatchIdCrash(Exception __exception, ref string __result)
        {
            if (__exception != null)
            {
                Logger.Log(LogLevel.Error | LogLevel.Message, "User ID file was corrupted, you might no longer have access to the cards you uploaded to the uploader.");
                Logger.LogError(__exception);
                __result = string.Empty;
            }

            return null;
        }
    }
}
