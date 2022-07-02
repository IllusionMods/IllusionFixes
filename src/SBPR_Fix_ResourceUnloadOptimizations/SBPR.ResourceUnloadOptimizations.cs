using BepInEx;
using Common;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessName32bit)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class ResourceUnloadOptimizations : BaseUnityPlugin
    {
        public const string GUID = "SBPR_Fix_ResourceUnloadOptimizations";

        private static Coroutine _currentCleanupAfterLoading = null;

        private static IEnumerator CleanupAfterAsyncLoading()
        {
            yield return null;
            while (GetIsNowLoadingFade())
            {
                yield return null;
            }

            // force a single unload once large load operation finishes
            while (_currentOperation != null && !_currentOperation.isDone)
            {
                yield return null;
            }
            _currentOperation = _originalUnload();
            yield return null;
            // force GC
            GC.Collect(GC.MaxGeneration);
            _currentCleanupAfterLoading = null;
        }

        partial class Hooks
        {
            // deploy happens when:
            // - main game save is loaded
            // - Time of day changes
            // - entering main game map from another scene (h-scene, changing room, etc.)
            [HarmonyPostfix]
            [HarmonyPatch(typeof(SexyBeach.MapScene), nameof(SexyBeach.MapScene.Deploy))]
            public static void DeployPostfix(SexyBeach.MapScene __instance)
            {
                if (!OptimizeMemoryUsage.Value || __instance == null) return;
                if (_currentCleanupAfterLoading != null) __instance.StopCoroutine(_currentCleanupAfterLoading);
                _currentCleanupAfterLoading = __instance.StartCoroutine(CleanupAfterAsyncLoading());
            }

        }
    }
}
