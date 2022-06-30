using BepInEx.Configuration;
using Common;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using BepInEx;
using UnityEngine;

namespace IllusionFixes
{
    [BepInIncompatibility("BepInEx.ResourceUnloadOptimizations")]
    public partial class ResourceUnloadOptimizations
    {
        public const string PluginName = "Resource Unload Optimizations";

        private static AsyncOperation _currentOperation;
        private static Func<AsyncOperation> _originalUnload;

        private static int _garbageCollect;

        private static int _sceneLoadOperationsInProgress;
        private static bool _sceneLoadedOrReset;

        private float _waitTime;

        public static ConfigEntry<bool> DisableUnload { get; private set; }
        public static ConfigEntry<bool> OptimizeMemoryUsage { get; private set; }
        public static ConfigEntry<int> PercentMemoryThreshold { get; private set; }
        public static ConfigEntry<int> PercentMemoryThresholdDuringLoad { get; private set; }

#if !SBPR
        private const int DefaultPercentMemoryThresholdDuringLoad = 65;
        private const string PercentMemoryThresholdDuringLoadRelative = "lower";
#else
        private const int DefaultPercentMemoryThresholdDuringLoad = 80;
        private const string PercentMemoryThresholdDuringLoadRelative = "higher";
#endif

        internal void Awake()
        {
            DisableUnload = Config.Bind(Utilities.ConfigSectionTweaks, "Disable Resource Unload", false, new ConfigDescription("Disables all resource unloading. Requires large amounts of RAM or will likely crash your game.", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            OptimizeMemoryUsage = Config.Bind(Utilities.ConfigSectionTweaks, "Optimize Memory Usage", true, new ConfigDescription("Use more memory (if available) in order to load the game faster and reduce random stutter."));
            PercentMemoryThreshold = Config.Bind(Utilities.ConfigSectionTweaks, "Optimize Memory Threshold", 75, new ConfigDescription("Minimum amount of memory to be used before resource unloading will run.", null, new ConfigurationManagerAttributes {IsAdvanced = true}));
            PercentMemoryThresholdDuringLoad = Config.Bind(Utilities.ConfigSectionTweaks, "Optimize Memory Threshold During Load", DefaultPercentMemoryThresholdDuringLoad, new ConfigDescription($"Minimum amount of memory to be used during load before resource unloading will run (should be {PercentMemoryThresholdDuringLoadRelative} than 'Optimize Memory Threshold').", null, new ConfigurationManagerAttributes {IsAdvanced = true}));
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

            Harmony.CreateAndPatchAll(typeof(Hooks));
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
            var isLoading = GetStudioLoadedNewScene() || GetIsNowLoadingFade();
            var pageFileFree = mem.ullAvailPageFile / (float)mem.ullTotalPageFile;
            var plentyOfMemory = mem.dwMemoryLoad < (isLoading ? PercentMemoryThresholdDuringLoad.Value : PercentMemoryThreshold.Value) // physical memory free %
                                 && pageFileFree > 0.3f // page file free %
                                 && mem.ullAvailPageFile > 2ul * 1024ul * 1024ul * 1024ul; // at least 2GB of page file free
            if (!plentyOfMemory)
            {
                // in case a previous scene load crashed leaving count incorrect, clean it up
                _sceneLoadOperationsInProgress = 0;
                return false;
            }

            Utilities.Logger.LogDebug($"Skipping cleanup because of low memory load ({mem.dwMemoryLoad}% RAM, {100 - (int)(pageFileFree * 100)}% Page file, {mem.ullAvailPageFile / 1024 / 1024}MB available in PF)");
            return true;
        }

        private static bool GetIsNowLoadingFade()
        {
#if HS2 || KKS
            return Manager.Scene.IsNowLoadingFade;
#elif PH
            return true;
#elif SBPR
            if (Constants.InsideStudio) return false;
            // SBPR main game loading detection is a bit more convoluted since large async loads
            // happen at during live game play
            return !Manager.Scene.IsInstance() || !Manager.Map.IsInstance() || !Manager.MapScene.IsInstance() ||
                   Manager.Map.Instance.NowLoading || Manager.MapScene.Instance.MapSceneClass == null ||
                   Manager.MapScene.Instance.MapSceneClass.IsNowLoading || Manager.Scene.Instance.sceneFade == null ||
                   Manager.Scene.Instance.sceneFade.IsFadeNow;
#else
            return !Manager.Scene.IsInstance() || Manager.Scene.Instance.IsNowLoadingFade;
#endif
        }

        private static bool GetStudioLoadedNewScene()
        {
            if (Constants.InsideStudio && _sceneLoadedOrReset)
            {
                _sceneLoadedOrReset = false;
                return true;
            }
            return false;
        }

        private static IEnumerator SceneLoadComplete()
        {
            yield return null;
            if (--_sceneLoadOperationsInProgress > 0) yield break;
            _sceneLoadOperationsInProgress = 0;
            _sceneLoadedOrReset = true;
        }

        private static partial class Hooks
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

#if !EC && !SBPR
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.LoadScene))]
            [HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.ImportScene))]
            [HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.InitScene))]
#if !HS && !PH && !SBPR
            [HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.LoadSceneCoroutine))]
#endif
            public static void LoadScenePrefix()
            {
                _sceneLoadOperationsInProgress++;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.LoadScene))]
            [HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.ImportScene))]
            [HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.InitScene))]
            public static void LoadScenePostfix()
            {
                try
                {
                    Studio.Studio.Instance.StartCoroutine(SceneLoadComplete());
                }
                catch
                {
                    _sceneLoadOperationsInProgress = 0;
                }
            }

#if !PH && !HS
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.LoadSceneCoroutine))]
            public static void LoadSceneCoroutinePostfix(ref IEnumerator __result)
            {
                // Setup a coroutine postfix
                var original = __result;
                __result = new[]
                {
                    original,
                    SceneLoadComplete()
                }.GetEnumerator();
            }
#endif // !PH && !HS

#endif // !EC
        }
    }
}
