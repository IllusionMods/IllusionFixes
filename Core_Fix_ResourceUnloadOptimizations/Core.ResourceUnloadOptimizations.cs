using BepInEx.Logging;
using Common;
using System;

namespace IllusionFixes
{
    public partial class ResourceUnloadOptimizations
    {
        public const string PluginName = "Resource Unload Optimizations";

        private static ResourceUnloadOptimizations _instance;

        private void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            _instance = this;
            Hooks.InstallHooks();
        }

        // Needs to be an instance method for Invoke to work
        private void RunGarbageCollect()
        {
            Utilities.Logger.Log(LogLevel.Debug, "Running full garbage collection");
            // Use different overload since we disable the parameterless one
            GC.Collect(GC.MaxGeneration);
        }
    }
}
