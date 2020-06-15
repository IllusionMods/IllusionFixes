using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Common;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IllusionFixes
{
    public partial class MakerOptimizations
    {
        public const string PluginName = "Maker Optimizations";

        public static ConfigEntry<bool> DisableNewAnimation { get; private set; }
        public static ConfigEntry<bool> DisableNewIndicator { get; private set; }
        public static ConfigEntry<bool> DisableCharaName { get; private set; }
        public static ConfigEntry<bool> DisableHiddenTabs { get; private set; }
        public static ConfigEntry<bool> ManageCursor { get; private set; }

        public MakerOptimizations()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            DisableNewAnimation = Config.Bind(Utilities.ConfigSectionTweaks, "Disable NEW indicator animation", true, new ConfigDescription("Good performance improvement in maker if there are many new items.\nChanges take effect after maker restart."));
            DisableNewIndicator = Config.Bind(Utilities.ConfigSectionTweaks, "Disable NEW indicator for new items", true, new ConfigDescription("visual glitches like 2 coordinates loaded at once."));
            DisableCharaName = Config.Bind(Utilities.ConfigSectionTweaks, "Disable character name box in maker", true, new ConfigDescription("Hides the name box in the bottom right part of the maker, giving you a clearer look at the character."));
            DisableHiddenTabs = Config.Bind(Utilities.ConfigSectionTweaks, "Deactivate hidden tabs in maker", SystemInfo.processorFrequency < 2700, new ConfigDescription("Major performance improvement at the cost of slower switching between tabs in maker.\nChanges take effect after maker restart."));
            ManageCursor = Config.Bind(Utilities.ConfigSectionTweaks, "Manage cursor in maker", true, new ConfigDescription("Lock and hide the cursor when moving the camera in maker."));
        }

        internal void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            SceneManager.sceneLoaded += SceneLoaded;

            DisableCharaName.SettingChanged += (sender, args) =>
            {
                if(FindObjectOfType<CustomScene>())
                    ToggleCharaName();
            };

            Hooks.InstallHooks();
        }

        internal void Start()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var cursorManager = gameObject.GetComponent<CursorManager>();
            if (FindObjectOfType<CustomScene>())
            {
                ToggleCharaName();
                if (!cursorManager) gameObject.AddComponent<CursorManager>();
            }
            else if (cursorManager)
            {
                Destroy(cursorManager);
            }
        }

        private void ToggleCharaName()
        {
            GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsCharaName")?.SetActive(!DisableCharaName.Value);
        }
    }
}
