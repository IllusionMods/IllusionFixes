using System;
using BepInEx;
using HarmonyLib;
using Studio;
using System.Diagnostics;

namespace IllusionFixes
{
    /// <summary>
    /// Outputs scene load times to log
    /// </summary>
    public partial class StudioOptimizations : BaseUnityPlugin
    {
        private static Stopwatch _stopwatch;

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
            Logger.LogInfo("Scene loading started.");
            _stopwatch = Stopwatch.StartNew();
        }

        static private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene arg0)
        {
            if (_stopwatch != null && arg0.name == "StudioSceneLoad")
            {
                double sec = _stopwatch.Elapsed.TotalSeconds;
                _stopwatch = null;
                Logger.LogInfo($"Scene loading completed: {sec:F1}[s]");
            }
        }
    }
}
