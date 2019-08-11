using BepInEx;
using BepInEx.Logging;
using Common;
using System;

namespace KK_Fix_ResourceUnloadOptimizations
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class ResourceUnloadOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ResourceUnloadOptimizations";
        public const string PluginName = "Resource Unload Optimizations";
        internal static new ManualLogSource Logger;

        private static ResourceUnloadOptimizations _instance;

        private void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            _instance = this;
            Logger = base.Logger;

            Hooks.InstallHooks();
        }

        // Needs to be an instance method for Invoke to work
        private void RunGarbageCollect()
        {
            Logger.Log(LogLevel.Debug, "[ResourceUnloadOptimizations] Running full garbage collection");
            // Use different overload since we disable the parameterless one
            GC.Collect(GC.MaxGeneration);
        }
    }
}
