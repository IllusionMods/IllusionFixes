using System;
using BepInEx;
using Common;
using Harmony;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace KK_Fix_ResourceUnloadOptimizations
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class ResourceUnloadOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ResourceUnloadOptimizations";
        public const string PluginName = "Resource Unload Optimizations";

        private void Awake()
        {
            Hooks.InstallHooks();
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
            }

            // Replacement methods needs to be inside a static class to be used in NativeDetour
            private static AsyncOperation UnloadUnusedAssetsHook()
            {
                if (_currentOperation == null || _currentOperation.isDone)
                    _currentOperation = _originalUnload();

                return _currentOperation;
            }
        }
    }
}
