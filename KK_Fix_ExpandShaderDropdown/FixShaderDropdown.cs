using BepInEx;
using ChaCustom;
using Common;
using Config;
using HarmonyLib;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_Fix_ExpandShaderDropdown
{
    // Based on koikoi.happy.nu.fix_shader_dropdown
    [BepInPlugin(GUID, "Fix Shader Dropdown Menu", Version)]
    public class ExpandShaderDropdown : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ExpandShaderDropdown";
        public const string Version = Metadata.PluginsVersion;

        private void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;
            if (CommonCode.InsideStudio) return;

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
