using System;
using System.IO;
using System.Reflection;
using ADV;
using AIChara;
using BepInEx;
using BepInEx.Logging;
using CharaCustom;
using Common;
using HarmonyLib;
using Manager;
using Studio;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class DataCorruptionFixes : BaseUnityPlugin
    {
        public const string GUID = "AI_Fix_LoadingFixes";
        public const string PluginName = "Studio and Maker Data Corruption Fixes";

        private static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(DataCorruptionFixes));
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(GameSystem), nameof(GameSystem.LoadNetworkSetting))]
        [HarmonyPatch(typeof(GameSystem), nameof(GameSystem.LoadDownloadInfo))]
        [HarmonyPatch(typeof(GameSystem), nameof(GameSystem.LoadApplauseInfo))]
        [HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.LoadItemID))] // seen items
        [HarmonyPatch(typeof(CustomBase.CustomSettingSave), nameof(CustomBase.CustomSettingSave.Load))] // maker config
        private static Exception CatchCrash(MethodBase __originalMethod, Exception __exception)
        {
            if (__exception != null)
            {
                Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, $"Corrupted save file detected in {__originalMethod.DeclaringType?.Name}, some progress might be lost.");
                Logger.LogWarning(__exception);
            }

            return null;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(TextScenario), nameof(TextScenario.LoadReadInfo))] // already read adv text
        private static Exception CatchReadInfoCrash(Exception __exception)
        {
            if (__exception != null)
            {
                Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, "ADV memory file was corrupted, you might no longer be able to skip previously read text.");
                Logger.LogWarning(__exception);

                File.Delete(Path.Combine(UserData.Path, "save/read.dat"));
                // Try again, this time it'll create a blank state since file doesn't exist
                TextScenario.LoadReadInfo();
            }

            return null;
        }

        /// <summary>
        /// Fixes broken color presets files breaking maker color picker
        /// </summary>
        [HarmonyPrefix]
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
                if (info == null) throw new InvalidDataException("Data is in invalid format");
            }
            catch (Exception ex)
            {
                File.Delete(path);
                Logger.LogWarning("ColorPresets.json file was corrupted and had to be removed. Saved color presets will be lost. Cause: " + ex.Message);
            }
        }

        /// <summary>
        /// Fixes color presets with an invalid selected index breaking maker color picker
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UI_ColorPresets), nameof(UI_ColorPresets.LoadPresets))]
        private static void LoadPresetsPostfix(UI_ColorPresets.ColorInfo ___colorInfo)
        {
            if (___colorInfo.select < 0 || ___colorInfo.select > 3)
                ___colorInfo.select = 0;
        }

        /// <summary>
        /// Fixes missing studio items crashing scene loading
        /// private static Info.ItemLoadInfo GetLoadInfo(int _group, int _category, int _no)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AddObjectItem), "GetLoadInfo", typeof(int), typeof(int), typeof(int))]
        private static void GetLoadInfoPrefix(int _group, int _category, ref int _no)
        {
            if (_group == 0 && _category == 0 && _no == 0)
                _no = 399;
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

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
        private static Exception CatchCorruptedCardCrash(ChaFile __instance, ref bool __result, Exception __exception, BinaryReader br)
        {
            if (__exception != null)
            {
                var source = br.BaseStream as FileStream;

                Logger.Log(BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message, "Corrupted character card: " + (source?.Name ?? "Unknown"));
                Logger.LogWarning(__exception);

                __result = false;
                __instance.lastLoadErrorCode = -69;
            }

            return null;
        }
    }
}
