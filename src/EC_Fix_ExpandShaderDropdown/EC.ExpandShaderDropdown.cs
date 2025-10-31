using System.Collections;
using System.Linq;
using BepInEx;
using ChaCustom;
using Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class ExpandShaderDropdown : BaseUnityPlugin
    {
        public const string GUID = "EC_Fix_ExpandShaderDropdown";
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

            var sceneMan = Singleton<Manager.Scene>.Instance;
            if (sceneMan.NowSceneNames.Any(sName => sName == "CustomScene"))
            {
                var tmpDropdown = Singleton<CustomConfig>.Instance.ddRamp;
                tmpDropdown.template.pivot = new Vector2(0.5f, 0f);
                tmpDropdown.template.anchorMin = new Vector2(0f, 0.86f);
            }
            else if (sceneMan.NowSceneNames.Any(sName => sName == "Config"))
            {
                var tmpDropdown = Singleton<Config.GraphicSetting>.Instance.rampIDDropdown;
                tmpDropdown.template.pivot = new Vector2(0.5f, 0f);
                tmpDropdown.template.anchorMin = new Vector2(0f, 0.86f);
            }
        }
    }
}
