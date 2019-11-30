using System;
using System.Collections;
using BepInEx.Harmony;
using BepInEx.Logging;
using Common;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace IllusionFixes
{
    public partial class ResourceUnloadOptimizations
    {
        public const string PluginName = "Resource Unload Optimizations";

        private static AsyncOperation _currentOperation;
        private static Func<AsyncOperation> _originalUnload;

        private static int _unloadAssets;
        private static int _garbageCollect;

        internal void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            StartCoroutine(CleanupCo());

            InstallHooks();
        }

        private static void InstallHooks()
        {
            var target = AccessTools.Method(typeof(Resources), nameof(Resources.UnloadUnusedAssets));
            var replacement = AccessTools.Method(typeof(Hooks), nameof(Hooks.UnloadUnusedAssetsHook));

            var detour = new NativeDetour(target, replacement);
            detour.Apply();

            _originalUnload = detour.GenerateTrampoline<Func<AsyncOperation>>();

            HarmonyWrapper.PatchAll(typeof(Hooks));
        }

        private IEnumerator CleanupCo()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1);

                if (_unloadAssets > 0)
                {
                    if (--_unloadAssets == 0)
                        RunUnloadAssets();
                }

                if (_garbageCollect > 0)
                {
                    if (--_garbageCollect == 0)
                        RunGarbageCollect();
                }
            }
        }

        private static void RunUnloadAssets()
        {
            if (_currentOperation?.isDone == false) return;

            Utilities.Logger.Log(LogLevel.Debug, "Starting unused asset cleanup");
            _currentOperation = _originalUnload();
        }

        private static void RunGarbageCollect()
        {
            Utilities.Logger.Log(LogLevel.Debug, "Starting full garbage collection");
            // Use different overload since we disable the parameterless one
            GC.Collect(GC.MaxGeneration);
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GC), nameof(GC.Collect), new Type[0])]
            public static bool GCCollectHook()
            {
                // Throttle down the calls. Keep resetting the timer until things calm down since it's usually fairly low memory usage
                _garbageCollect = 3;
                // Disable the original method, Invoke will call it later
                return false;
            }

            // Replacement methods needs to be inside a static class to be used in NativeDetour
            public static AsyncOperation UnloadUnusedAssetsHook()
            {
                if (_currentOperation == null)
                    RunUnloadAssets();
                // Throttle down the calls but make sure we run at least every x seconds to avoid temporary memory overruns
                else if (_currentOperation.isDone && _unloadAssets <= 0)
                    _unloadAssets = 3;

                return _currentOperation;
            }
        }
    }
}
