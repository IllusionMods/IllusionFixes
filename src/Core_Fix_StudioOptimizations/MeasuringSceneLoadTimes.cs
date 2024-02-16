using System;
using BepInEx.Logging;
using HarmonyLib;
using Studio;

namespace IllusionFixes
{
    /// <summary>
    /// Outputs scene load times to log
    /// </summary>
    class MeasuringLoadTimes
    {
        private static ManualLogSource Logger;

        private static DateTime _startLoading = new DateTime();
        private static bool _loading = false;

        public static void Setup( ManualLogSource logger )
        {
            Logger = logger;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
            Harmony.CreateAndPatchAll(typeof(MeasuringLoadTimes), nameof(MeasuringLoadTimes));
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad")]
        private static void OnClickLoadPrefix()
        {
            OnStartLoadScene();
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(SceneLoadScene), "OnClickImport")]
        private static void OnClickImportPrefix()
        {
            OnStartLoadScene();
        }

        static private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene arg0)
        {
            if (_loading && arg0.name == "StudioSceneLoad")
            {
                _loading = false;
                DateTime end = DateTime.Now;
                double sec = (end - _startLoading).TotalSeconds;
                Logger.LogInfo($"Scene Loaded: {sec:F2}[s]");
            }
        }

        static private void OnStartLoadScene()
        {
            _startLoading = System.DateTime.Now;
            _loading = true;
        }
    }
}
