using System;
using System.Collections;
using BepInEx;
using ChaCustom;
using Common;
using Config;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class ExpandShaderDropdown : BaseUnityPlugin
    {
        public const string GUID = "KKS_Fix_ExpandShaderDropdown";
        public const string PluginName = "Fix Shader Dropdown Menu";

        private void Start()
        {
            if (Constants.InsideStudio)
                StartCoroutine(StudioDropdownFix());
            else
                Harmony.CreateAndPatchAll(typeof(ExpandShaderDropdown), GUID);
        }

        private static void AdjustTemplateSize(TMP_Dropdown dropdown, float size) => dropdown.template.sizeDelta = new Vector2(0f, size);
        private static void AdjustTemplateSize(Dropdown dropdown, int size) => dropdown.template.sizeDelta = new Vector2(0f, size);

        private static IEnumerator StudioDropdownFix()
        {
            yield return new WaitUntil(Studio.Studio.IsInstance);
            AdjustTemplateSize(Studio.Studio.Instance.systemButtonCtrl.amplifyColorEffectInfo.dropdownLut, 950);
            AdjustTemplateSize(Studio.Studio.Instance.systemButtonCtrl.etcInfo.dropdownRamp, 740);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomConfig), nameof(CustomConfig.CalculateUI))]
        private static void MakerDropdownFix(CustomConfig __instance)
        {
            AdjustTemplateSize(__instance.ddRamp, 800f);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GraphicSetting), nameof(GraphicSetting.Init))]
        private static void ConfigDropdownFix(GraphicSetting __instance)
        {
            AdjustTemplateSize(__instance.rampIDDropdown, 700f);
        }
    }
}
