using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using Common;
using HarmonyLib;
using Studio;
using System;
using System.IO;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class DataCorruptionFixes : BaseUnityPlugin
    {
        public const string GUID = "HS2_Fix_LoadingFixes";
        public const string PluginName = "Studio and Maker Data Corruption Fixes";

        private static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            HarmonyWrapper.PatchAll(typeof(DataCorruptionFixes));
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
    }
}
