﻿using ADV;
using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using Common;
using HarmonyLib;
using Studio;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class DataCorruptionFixes : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_DataCorruptionFixes";
        public const string PluginName = "Game and Studio Data Corruption Fixes";

        private static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            var h = Harmony.CreateAndPatchAll(typeof(DataCorruptionFixes));

            // Only exists in KKParty
            var td = Type.GetType("TutorialData, Assembly-CSharp", false);
            if (td != null)
                h.Patch(td.GetMethod("Load", AccessTools.all), finalizer: new HarmonyMethod(typeof(DataCorruptionFixes), nameof(CatchCrash)));
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(GlobalSaveData), nameof(GlobalSaveData.Load))]
        [HarmonyPatch(typeof(SaveData), nameof(SaveData.Load), typeof(string), typeof(string))]
        [HarmonyPatch(typeof(RankingData), nameof(RankingData.Load))]
        [HarmonyPatch(typeof(WeddingData), nameof(WeddingData.Load))]
        [HarmonyPatch(typeof(MemoriesData), nameof(MemoriesData.Load))]
        [HarmonyPatch(typeof(DownloadScene), nameof(DownloadScene.LoadCacheSetting))] // uploader card caching setting
        [HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.LoadItemID))] // seen items
        [HarmonyPatch(typeof(CustomBase.CustomSettingSave), nameof(CustomBase.CustomSettingSave.Load))] // maker config
        private static Exception CatchCrash(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null)
            {
                Logger.Log(LogLevel.Warning | LogLevel.Message, $"Corrupted save file detected in {__originalMethod.DeclaringType?.Name}, some progress might be lost.");
                Logger.LogWarning(__exception);
            }

            return null;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(MainScenario), nameof(MainScenario.LoadReadInfo))] // already read adv text
        private static Exception CatchReadInfoCrash(Exception __exception)
        {
            if (__exception != null)
            {
                Logger.Log(LogLevel.Warning | LogLevel.Message, "ADV memory file was corrupted, you might no longer be able to skip previously read text.");
                Logger.LogWarning(__exception);

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
                Logger.Log(LogLevel.Warning | LogLevel.Message, "User ID file was corrupted, you might no longer have access to the cards you uploaded to the uploader.");
                Logger.LogWarning(__exception);
                __result = string.Empty;
            }

            return null;
        }

        /// <summary>
        /// Fixes broken color presets files breaking maker color picker
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ColorPresets), nameof(ColorPresets.LoadPresets))]
        [HarmonyPatch(typeof(UI_ColorPresets), nameof(UI_ColorPresets.LoadPresets))]
        private static void LoadPresetsPrefix(UI_ColorPresets __instance, string ___saveDir)
        {
            var path = ___saveDir + "ColorPresets.json";

            if (!File.Exists(path))
                return;

            try
            {
                var json = File.ReadAllText(path);
                var info = JsonUtility.FromJson<UI_ColorPresets.ColorInfo>(json);
                if (info == null) throw new Exception("Data is in invalid format");
            }
            catch (Exception ex)
            {
                File.Delete(path);
                Logger.Log(LogLevel.Warning | LogLevel.Message, "ColorPresets.json file was corrupted and had to be removed. Saved color presets will be lost.");
                Logger.LogWarning(ex);
            }
        }

        /// <summary>
        /// Fixes scenes failing to load because of corrupted json color data
        /// public static object FromJson(string json, Type type)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(JsonUtility), nameof(JsonUtility.FromJson), typeof(string), typeof(Type))]
        private static void FromJsonPrefix(ref string json)
        {
            var i = json.IndexOf('{');
            var i2 = json.LastIndexOf('}');
            if (i > 0 && i2 > 0 && i2 > i)
            {
                Logger.LogWarning("Mangled Json data detected, attempting to fix - " + json);
                json = json.Substring(i, i2 - i + 1);
            }
            else if (i < 0 || i2 < 0 || i2 < i)
            {
                Logger.LogWarning("Invalid Json data detected");
            }
        }
    }
}
