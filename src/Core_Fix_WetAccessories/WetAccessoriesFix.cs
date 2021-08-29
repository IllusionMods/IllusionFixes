using System;
using AIChara;
using BepInEx;
using Common;
using HarmonyLib;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class WetAccessoriesFix : BaseUnityPlugin
    {
        public const string GUID = "Fix_WetAccessoriesFix";
        public const string PluginName = "Wet Accessories Fix";

        private void Awake()
        {
            //TODO Reenable once things work properly
            //Harmony.CreateAndPatchAll(typeof(WetAccessoriesFix));
            Logger.LogWarning("This plugin is temporarily disabled because of some accessory zipmods crashing the game");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateWet))]
        private static void FixWetAccs(ChaControl __instance)
        {
            try
            {
                var wetRate = __instance.fileStatus.wetRate;

                foreach (var accessory in __instance.cmpAccessory)
                {
                    if (accessory == null) continue;
                    ApplyWetRate(accessory.rendNormal, wetRate);
                    ApplyWetRate(accessory.rendAlpha, wetRate);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        private static void ApplyWetRate(Renderer[] renderers, float wetRate)
        {
            if (renderers == null) return;
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;
                    if (material.HasProperty(ChaShader.wetRate))
                    {
                        material.SetFloat(ChaShader.wetRate, wetRate);
                    }
                }
            }
        }
    }
}
