using System;
using BepInEx;
using HarmonyLib;
using Studio;

namespace IllusionFixes
{
    /// <summary>
    /// Outputs scene load times to log
    /// </summary>
    public partial class StudioOptimizations : BaseUnityPlugin
    {
        private static DateTime _startLoading = new DateTime();
        private static bool _loading = false;

        static private void SetupMeasuringSceneLoad()
        {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(SceneLoadScene), nameof(SceneLoadScene.OnClickLoad))]
        [HarmonyPatch(typeof(SceneLoadScene), nameof(SceneLoadScene.OnClickImport))]
        static private void OnStartLoadScene()
        {
            Logger.LogInfo("Start load/import scene");
            _startLoading = System.DateTime.Now;
            _loading = true;
        }

        static private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene arg0)
        {
            if (_loading && arg0.name == "StudioSceneLoad")
            {
                _loading = false;
                DateTime end = DateTime.Now;
                double sec = (end - _startLoading).TotalSeconds;
                Logger.LogInfo($"Scene Loaded: {sec:F1}[s]");
            }
        }
    }
}
