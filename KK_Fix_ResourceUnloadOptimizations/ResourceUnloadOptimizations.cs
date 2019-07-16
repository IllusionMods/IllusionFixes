using System;
using BepInEx;
using BepInEx.Logging;
using Common;
using Harmony;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_Fix_ResourceUnloadOptimizations
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class ResourceUnloadOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ResourceUnloadOptimizations";
        public const string PluginName = "Resource Unload Optimizations";

        private static ResourceUnloadOptimizations _instance;

        private void Awake()
        {
            _instance = this;

            Hooks.InstallHooks();
        }

        // Needs to be an instance method for Invoke to work
        private void RunGarbageCollect()
        {
            Logger.Log(LogLevel.Debug, "[ResourceUnloadOptimizations] Running full garbage collection");
            // Use different overload since we disable the parameterless one
            GC.Collect(GC.MaxGeneration);
        }

        private static class Hooks
        {
            private static AsyncOperation _currentOperation;
            private static Func<AsyncOperation> _originalUnload;

            public static void InstallHooks()
            {
                var target = AccessTools.Method(typeof(Resources), nameof(Resources.UnloadUnusedAssets));
                var replacement = AccessTools.Method(typeof(Hooks), nameof(UnloadUnusedAssetsHook));

                var detour = new NativeDetour(target, replacement);
                detour.Apply();

                _originalUnload = detour.GenerateTrampoline<Func<AsyncOperation>>();

                HarmonyInstance.Create(GUID).PatchAll(typeof(Hooks));
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(GC), nameof(GC.Collect), new Type[0])]
            public static bool GCCollectHook()
            {
                // Throttle down the calls
                _instance.CancelInvoke(nameof(RunGarbageCollect));
                _instance.Invoke(nameof(RunGarbageCollect), 3f);
                // Disable the original method, Invoke will call it later
                return false;
            }

            // Replacement methods needs to be inside a static class to be used in NativeDetour
            private static AsyncOperation UnloadUnusedAssetsHook()
            {
                if (_currentOperation == null || _currentOperation.isDone)
                {
                    Logger.Log(LogLevel.Debug, "[ResourceUnloadOptimizations] Starting unused resource cleanup");
                    _currentOperation = _originalUnload();
                }

                return _currentOperation;
            }
        }
    }
}
