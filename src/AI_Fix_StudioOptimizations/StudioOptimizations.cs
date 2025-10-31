using System;
using System.Linq;
using BepInEx;
using Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class StudioOptimizations : BaseUnityPlugin
    {
        private void Start()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            try
            {
                if (!Studio.GuideObjectManager.IsInstance()) return;

                // Fix rotation gizmo center being disabled for some reason
                var rotateObj = Studio.GuideObjectManager.Instance.objectOriginal?.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(y => y.name == "XYZ" && y.parent.name == "rotation");
                if (rotateObj != null)
                {
                    rotateObj.gameObject.SetActive(true);
                    SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex);
            }
        }
    }
}
