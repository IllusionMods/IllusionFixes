using BepInEx.Configuration;
using ChaCustom;
using Common;
using Config;
using HarmonyLib;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IllusionFixes
{
    // Based on koikoi.happy.nu.fix_shader_dropdown
    public partial class ExpandShaderDropdown
    {
        public const string PluginName = "Fix Shader Dropdown Menu";

        internal void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;
            if (CommonCode.InsideStudio) return;
            if (!Config.AddSetting(Utilities.ConfigSectionFixes, "Fix shader dropdown menu", true,
                new ConfigDescription("Fixes the shader selection menu going off-screen when there are many modded shaders installed.")).Value)
                return;

            SceneManager.sceneLoaded += (s, m) => StartCoroutine(DelayedStart());
        }

        private static IEnumerator DelayedStart()
        {
            // Needed because party localization makes UI load late
            yield return null;

            if (Singleton<Manager.Scene>.Instance.NowSceneNames.Any(sName => sName == "Config"))
            {
                var tmpDropdown = Traverse.Create(Singleton<GraphicSetting>.Instance).Field("rampIDDropdown").GetValue<TMP_Dropdown>();
                tmpDropdown.template.pivot = new Vector2(0.5f, 0f);
                tmpDropdown.template.anchorMin = new Vector2(0f, 0.86f);
            }
            else if (Singleton<Manager.Scene>.Instance.NowSceneNames.Any(sName => sName == "CustomScene"))
            {
                var tmpDropdown = Traverse.Create(Singleton<CustomConfig>.Instance).Field("ddRamp").GetValue<TMP_Dropdown>();
                tmpDropdown.template.pivot = new Vector2(0.5f, 0f);
                tmpDropdown.template.anchorMin = new Vector2(0f, 0.86f);
            }
        }
    }
}
