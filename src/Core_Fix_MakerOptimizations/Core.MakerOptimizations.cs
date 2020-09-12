using BepInEx.Configuration;
using Common;
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
        private static ConfigEntry<int> ListWidth { get; set; }

        public MakerOptimizations()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            DisableNewAnimation = Config.Bind(Utilities.ConfigSectionTweaks, "Disable NEW indicator animation", true, new ConfigDescription("Good performance improvement in maker if there are many new items.\nChanges take effect after maker restart."));
            DisableNewIndicator = Config.Bind(Utilities.ConfigSectionTweaks, "Disable NEW indicator for new items", true, new ConfigDescription("visual glitches like 2 coordinates loaded at once."));
            DisableCharaName = Config.Bind(Utilities.ConfigSectionTweaks, "Disable character name box in maker", true, new ConfigDescription("Hides the name box in the bottom right part of the maker, giving you a clearer look at the character."));
            DisableHiddenTabs = Config.Bind(Utilities.ConfigSectionTweaks, "Disable hidden tabs in maker", true, new ConfigDescription("Major performance improvement in chara maker.\nRecommended to be used together with list virtualization, otherwise switching between tabs becomes slower.\nChanges take effect after maker restart."));
            ManageCursor = Config.Bind(Utilities.ConfigSectionTweaks, "Manage cursor in maker", true, new ConfigDescription("Lock and hide the cursor when moving the camera in maker."));
            ListWidth = Config.Bind(Utilities.ConfigSectionTweaks, "Width of maker item lists", 3, new ConfigDescription("How many items fit horizontally in a single row of the item lists in character maker.\n Changes require a restart of character maker.", new AcceptableValueRange<int>(3, 8)));
            var virtualize = Config.Bind(Utilities.ConfigSectionTweaks, "Virtualize maker lists", true, "Major load time reduction and performance improvement in character maker. Eliminates lag when switching tabs.\nCan cause some compatibility issues with other plugins.\nChanges take effect after game restart.");
            
            if (virtualize.Value) VirtualizeMakerLists.InstallHooks();
        }


        internal void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            SceneManager.sceneLoaded += SceneLoaded;

            DisableCharaName.SettingChanged += (sender, args) =>
            {
                if (FindObjectOfType<CustomScene>())
                    ToggleCharaName();
            };

            Hooks.InstallHooks();
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
