using System.Collections;
using System.Linq;
using BepInEx;
using Common;
using Config;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInProcess(Constants.VRProcessName)]
    [BepInProcess(Constants.VRProcessNameSteam)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class ExpandShaderDropdown : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ExpandShaderDropdown";
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

            var sceneMan = Singleton<Manager.Scene>.Instance;
            if (sceneMan.NowSceneNames.Any(sName => sName == "Config"))
            {
                var tmpDropdown = Traverse.Create(Singleton<GraphicSetting>.Instance).Field("rampIDDropdown").GetValue<TMP_Dropdown>();
                tmpDropdown.template.pivot = new Vector2(0.5f, 0f);
                tmpDropdown.template.anchorMin = new Vector2(0f, 0.86f);

                // Fix for broken list when opening config from charamaker
                yield return new WaitWhile(() => tmpDropdown && !tmpDropdown.IsExpanded);
                if (tmpDropdown)
                {
                    foreach (var canvas in tmpDropdown.GetComponentsInChildren<Canvas>(true))
                        canvas.sortingOrder = 32030;
                }
            }
            else if (sceneMan.NowSceneNames.Any(sName => sName == "CustomScene"))
            {
                var tmpDropdown = Traverse.Create(Singleton<ChaCustom.CustomConfig>.Instance).Field("ddRamp").GetValue<TMP_Dropdown>();
                tmpDropdown.template.pivot = new Vector2(0.5f, 0f);
                tmpDropdown.template.anchorMin = new Vector2(0f, 0.86f);
            }
            else if (sceneMan.LoadSceneName == "Studio")
            {
                if (Studio.Studio.IsInstance())
                {
                    var traverse = Traverse.Create(Studio.Studio.Instance.systemButtonCtrl);
                    var tmpDropdownLut = traverse.Field("amplifyColorEffectInfo").Field("dropdownLut").GetValue<Dropdown>();
                    tmpDropdownLut.template.sizeDelta = new Vector2(0, 950);
                    var tmpDropdownRamp = traverse.Field("etcInfo").Field("dropdownRamp").GetValue<Dropdown>();
                    tmpDropdownRamp.template.sizeDelta = new Vector2(0, 800);
                }
            }
        }
    }
}
