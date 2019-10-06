using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Logging = BepInEx.Logging;

namespace Common
{
    internal class IncompatiblePluginDetector : MonoBehaviour
    {
        private static readonly string[] _pluginBlacklist =
        {
            "FixCompilation.dll",
            "FixShaderDropdown.dll",
            "KK_CutsceneLockupFix.dll",
            "KK_Fix_HeadFix.dll",
            "KK_Fix_ListFix.dll",
            "KK_Fix_MainGameOptimization.dll",
            "KK_Fix_MakerOptimization.dll",
            "KK_Fix_ResourceUnloadOptimization.dll",
            "KK_Fix_SettingsFix.dll",
            "KK_HeadFix.dll",
            "KK_MiscFixes.dll",
            "KK_PersonalityCorrector.dll",
            "KK_SettingsFix.dll",
            "MakerLag.dll"
        };

        private static bool? _incompatiblePlugsFound;
        private List<string> _badPlugins;

        public static bool AnyIncompatiblePlugins()
        {
            if (!_incompatiblePlugsFound.HasValue)
            {
                var badPlugins = new List<string>();
                _incompatiblePlugsFound = false;
                foreach (var pluginName in _pluginBlacklist)
                {
                    if (File.Exists(Path.Combine(Paths.PluginPath, pluginName)))
                    {
                        _incompatiblePlugsFound = true;
                        badPlugins.Add(pluginName);
                    }
                }

                if (_incompatiblePlugsFound.Value)
                {
                    const string goName = nameof(IncompatiblePluginDetector);
                    // Static state is not shared with a shared project so need to prevent running this multiple times in a different way
                    if (!GameObject.Find(goName))
                    {
                        // This is needed to make sure that MessageCenter is alive and listening before posting the error messages or they won't show up
                        var go = new GameObject(goName);
                        var mb = go.AddComponent<IncompatiblePluginDetector>();
                        mb._badPlugins = badPlugins;
                    }
                }
            }

            return _incompatiblePlugsFound.Value;
        }

        internal void Start()
        {
            if (_badPlugins.Any())
            {
                foreach (var pluginName in _badPlugins)
                    Utilities.Logger.Log(Logging.LogLevel.Error | Logging.LogLevel.Message, "ERROR - Outdated plugin detected, please remove it: " + pluginName);

                Utilities.Logger.LogMessage("After removing these plugins update to the latest version of KoikatuFixes");
            }

            Destroy(gameObject);
        }
    }
}
