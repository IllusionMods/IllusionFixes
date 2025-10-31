using System.Collections;
using BepInEx;
using Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class ExpandShaderDropdown : BaseUnityPlugin
    {
        public const string GUID = "AI_Fix_ExpandShaderDropdown";
        public const string PluginName = "Fix Shader Dropdown Menu";

        private void Awake()
        {
            SceneManager.sceneUnloaded += _ => StopAllCoroutines();
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
                    var tmpDropdownLut = Studio.Studio.Instance.systemButtonCtrl.colorGradingInfo.dropdownLookupTexture;
                    tmpDropdownLut.template.sizeDelta = new Vector2(0, 950);
                    var tmpDropdownRamp = Studio.Studio.Instance.systemButtonCtrl.reflectionProbeInfo.dropdownCubemap;
                    tmpDropdownRamp.template.sizeDelta = new Vector2(0, 800);
                }
            }
        }
    }
}
