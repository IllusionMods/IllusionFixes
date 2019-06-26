using System;
using BepInEx;
using BepInEx.Logging;
using Common;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_Fix_ResourceUnloadOptimizations
{
    /// <summary>
    /// Changes any invalid personalities to the "Pure" personality to prevent the game from breaking when adding them to the class
    /// </summary>
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class ResourceUnloadOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ResourceUnloadOptimizations";
        public const string PluginName = "Resource Unload Optimizations";

		private static Func<AsyncOperation> originalUnload;
		private static DateTime LastUnload = DateTime.Now;

        private void Awake()
        {
			var detour = new NativeDetour(typeof(Resources).GetMethod(nameof(Resources.UnloadUnusedAssets)),
				typeof(ResourceUnloadOptimizations).GetMethod(nameof(RunUnloadUnusedAssets)));

			detour.Apply();
			originalUnload = detour.GenerateTrampoline<Func<AsyncOperation>>();
        }

        private static AsyncOperation RunUnloadUnusedAssets()
        {
			if ((DateTime.Now - LastUnload).TotalSeconds >= 5)
			{
				LastUnload = DateTime.Now;
				Logger.Log(LogLevel.Info, "RunUnloadUnusedAssets");
				return originalUnload();
			}

			return null;
		}
    }
}
