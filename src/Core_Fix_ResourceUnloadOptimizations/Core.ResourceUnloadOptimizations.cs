using BepInEx.Configuration;
using BepInEx.Harmony;
using Common;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using UnityEngine;

namespace IllusionFixes
{
    public partial class ResourceUnloadOptimizations
    {
        public const string PluginName = "Resource Unload Optimizations";

        private static AsyncOperation _currentOperation;
        private static Func<AsyncOperation> _originalUnload;

        private static int _garbageCollect;
        private float _waitTime;

        public static ConfigEntry<bool> DisableUnload { get; private set; }
        public static ConfigEntry<bool> OptimizeMemoryUsage { get; private set; }

        internal void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            DisableUnload = Config.Bind(Utilities.ConfigSectionTweaks, "Disable Resource Unload", false, new ConfigDescription("Disables all resource unloading. Requires large amounts of RAM or will likely crash your game.", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            OptimizeMemoryUsage = Config.Bind(Utilities.ConfigSectionTweaks, "Optimize Memory Usage", true, new ConfigDescription("Use more memory (if available) in order to load the game faster and reduce random stutter."));

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
                while (Time.realtimeSinceStartup < _waitTime)
                    yield return null;

                _waitTime = Time.realtimeSinceStartup + 1;

                if (_garbageCollect > 0)
                {
                    if (--_garbageCollect == 0)
                        RunGarbageCollect();
                }
            }
        }

        private static AsyncOperation RunUnloadAssets()
        {
            // Only allow a single unload operation to run at one time
            if (_currentOperation == null || _currentOperation.isDone && !PlentyOfMemory())
            {
                Utilities.Logger.LogDebug("Starting unused asset cleanup");
                _currentOperation = _originalUnload();
            }
            return _currentOperation;
        }

        private static void RunGarbageCollect()
        {
            if (PlentyOfMemory()) return;

            Utilities.Logger.LogDebug("Starting full garbage collection");
            // Use different overload since we disable the parameterless one
            GC.Collect(GC.MaxGeneration);
        }

        private static bool PlentyOfMemory()
        {
            if (!OptimizeMemoryUsage.Value) return false;

            var mem = MemoryInfo.GetCurrentStatus();
            if (mem == null) return false;

            // Clean up more aggresively during loading, less aggresively during gameplay
            var isLoading = GetIsNowLoadingFade();
            var pageFileFree = mem.ullAvailPageFile / (float)mem.ullTotalPageFile;
            var plentyOfMemory = mem.dwMemoryLoad < (isLoading ? 65 : 75) // physical memory free %
                                 && pageFileFree > 0.3f // page file free %
                                 && mem.ullAvailPageFile > 2ul * 1024ul * 1024ul * 1024ul; // at least 2GB of page file free
            if (!plentyOfMemory) return false;

            Utilities.Logger.LogDebug($"Skipping cleanup because of low memory load ({mem.dwMemoryLoad}% RAM, {100 - (int)(pageFileFree * 100)}% Page file, {mem.ullAvailPageFile / 1024 / 1024}MB available in PF)");
            return true;
        }

        private static bool GetIsNowLoadingFade()
        {
#if HS2
            return Manager.Scene.IsNowLoadingFade;
#elif PH
            return true;
#else
            return Manager.Scene.Instance.IsNowLoadingFade;
#endif
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

            // Replacement method needs to be inside a static class to be used in NativeDetour
            public static AsyncOperation UnloadUnusedAssetsHook()
            {
                if (DisableUnload.Value)
                    return null;
                else
                    return RunUnloadAssets();
            }
        }
    }
}
