using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IllusionFixes
{
    [BepInDependency("com.joan6694.illusionplugins.moreaccessories", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.essu.stiletto", BepInDependency.DependencyFlags.SoftDependency)]
    public partial class MakerOptimizations
    {
        public const string PluginName = "Maker Optimizations";

        public static ConfigEntry<bool> DisableNewAnimation { get; private set; }
        public static ConfigEntry<bool> DisableNewIndicator { get; private set; }
        public static ConfigEntry<bool> DisableIKCalc { get; private set; }
        public static ConfigEntry<bool> DisableCharaName { get; private set; }
        public static ConfigEntry<bool> DisableHiddenTabs { get; private set; }
        public static ConfigEntry<bool> ManageCursor { get; private set; }
        private static ConfigEntry<int> ListWidth { get; set; }

        public MakerOptimizations()
        {
            var stilettoInstalled = BepInEx.Bootstrap.Chainloader.PluginInfos.Values.Any(x => x.Metadata.GUID == "com.essu.stiletto");

            DisableNewAnimation = Config.Bind(Utilities.ConfigSectionTweaks, "Disable NEW indicator animation", true, new ConfigDescription("Performance improvement in maker if there are many new items.\nChanges take effect after maker restart."));
            DisableNewIndicator = Config.Bind(Utilities.ConfigSectionTweaks, "Disable NEW indicator", false, new ConfigDescription("Turn off the New! mark on items in character maker that weren't used yet.\nChanges take effect after maker restart."));
            DisableIKCalc = Config.Bind(Utilities.ConfigSectionTweaks, "Disable IK in maker", !stilettoInstalled, new ConfigDescription("This setting prevents the character's limbs from being readjusted to match body proportions. It can fix weirdly bent limbs on characters that use ABMX sliders, but will break Stiletto if it's installed.\nWarning: This setting will get reset to false if Stiletto is installed to avoid issues!\nChanges take effect after game restart."));
            DisableCharaName = Config.Bind(Utilities.ConfigSectionTweaks, "Disable character name box in maker", true, new ConfigDescription("Hides the name box in the bottom right part of the maker, giving you a clearer look at the character."));
            DisableHiddenTabs = Config.Bind(Utilities.ConfigSectionTweaks, "Disable hidden tabs in maker", true, new ConfigDescription("Major performance improvement in chara maker.\nRecommended to be used together with list virtualization, otherwise switching between tabs becomes slower.\nChanges take effect after maker restart."));
            ManageCursor = Config.Bind(Utilities.ConfigSectionTweaks, "Manage cursor in maker", true, new ConfigDescription("Lock and hide the cursor when moving the camera in maker."));
            ListWidth = Config.Bind(Utilities.ConfigSectionTweaks, "Width of maker item lists", 3, new ConfigDescription("How many items fit horizontally in a single row of the item lists in character maker.\n Changes require a restart of character maker.", new AcceptableValueRange<int>(3, 8)));
            var virtualize = Config.Bind(Utilities.ConfigSectionTweaks, "Virtualize maker lists", true, "Major load time reduction and performance improvement in character maker. Eliminates lag when switching tabs.\nCan cause some compatibility issues with other plugins.\nChanges take effect after game restart.");

            if (virtualize.Value) VirtualizeMakerLists.InstallHooks();

            if (DisableIKCalc.Value && BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.essu.stiletto"))
                Logger.Log(LogLevel.Warning, "Stiletto is installed but Disable maker IK is enabled! Heels will not work properly in character maker until this setting is turned off");
        }

        internal void Awake()
        {
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
