using System.Collections;
using BepInEx;
using Common;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class ExpandShaderDropdown : BaseUnityPlugin
    {
        public const string GUID = "AI_Fix_ExpandShaderDropdown";
        public const string PluginName = "Fix Shader Dropdown Menu";

        private void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            SceneManager.sceneLoaded += (s, m) => StartCoroutine(DelayedStart());
        }

        private static IEnumerator DelayedStart()
        {
            // Needed to let UI load
            yield return null;

            if (Singleton<Manager.Scene>.Instance.LoadSceneName == "Studio")
            {
                if (Studio.Studio.IsInstance())
                {
                    var traverse = Traverse.Create(Studio.Studio.Instance.systemButtonCtrl);
                    var tmpDropdownLut = traverse.Field("colorGradingInfo").Field("dropdownLookupTexture").GetValue<Dropdown>();
                    tmpDropdownLut.template.sizeDelta = new Vector2(0, 950);
                    var tmpDropdownRamp = traverse.Field("reflectionProbeInfo").Field("dropdownCubemap").GetValue<Dropdown>();
                    tmpDropdownRamp.template.sizeDelta = new Vector2(0, 800);
                }
            }
        }
    }
}
