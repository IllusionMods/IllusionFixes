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
        public static ConfigEntry<bool> DisableIKCalc { get; private set; }
        public static ConfigEntry<bool> DisableCameraTarget { get; private set; }
        public static ConfigEntry<bool> DisableCharaName { get; private set; }
        public static ConfigEntry<bool> DisableHiddenTabs { get; private set; }
        public static ConfigEntry<bool> ManageCursor { get; private set; }

        public MakerOptimizations()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            DisableNewAnimation = Utilities.FixesConfig.AddSetting(Utilities.ConfigSectionTweaks, "Disable NEW indicator animation", true, new ConfigDescription("Good performance improvement in maker if there are many new items.\nChanges take effect after maker restart."));
            DisableNewIndicator = Utilities.FixesConfig.AddSetting(Utilities.ConfigSectionTweaks, "Disable NEW indicator for new items", true, new ConfigDescription("visual glitches like 2 coordinates loaded at once."));
            DisableIKCalc = Utilities.FixesConfig.AddSetting(Utilities.ConfigSectionTweaks, "Disable maker IK", true, new ConfigDescription("Improves performance and reduces stuttering at the cost of not recalculating positions of some body parts.\nMost noticeable on characters with wide hips where the hands are not moving with the hip line.\nWarning: This setting will get reset to false if Stiletto is installed to avoid issues!\nChanges take effect after game restart."));
            DisableCameraTarget = Utilities.FixesConfig.AddSetting(Utilities.ConfigSectionTweaks, "Disable camera target (white focus ring)", false, new ConfigDescription("Warning: This setting overrides any game setting that enables the ring."));
            DisableCharaName = Utilities.FixesConfig.AddSetting(Utilities.ConfigSectionTweaks, "Disable character name box in maker", true, new ConfigDescription("Hides the name box in the bottom right part of the maker, giving you a clearer look at the character."));
            DisableHiddenTabs = Utilities.FixesConfig.AddSetting(Utilities.ConfigSectionTweaks, "Deactivate hidden tabs in maker", SystemInfo.processorFrequency < 2700, new ConfigDescription("Major performance improvement at the cost of slower switching between tabs in maker.\nChanges take effect after maker restart."));
            ManageCursor = Utilities.FixesConfig.AddSetting(Utilities.ConfigSectionTweaks, "Manage cursor in maker", true, new ConfigDescription("Lock and hide the cursor when moving the camera in maker."));
        }

        internal void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            SceneManager.sceneLoaded += SceneLoaded;
            DisableCameraTarget.SettingChanged += (sender, args) => ApplyPatches();
            DisableCharaName.SettingChanged += (sender, args) => ApplyPatches();

            Hooks.InstallHooks();
        }

        internal void Start()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            if (DisableIKCalc.Value && BepInEx.Bootstrap.Chainloader.Plugins.Select(MetadataHelper.GetMetadata).Any(x => x.GUID == "com.essu.stiletto"))
            {
                DisableIKCalc.Value = false;
                Utilities.Logger.Log(LogLevel.Warning | LogLevel.Message, "Stiletto detected, disabling the DisableIKCalc optimization for compatibility");
            }
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyPatches();

            var cursorManager = gameObject.GetComponent<CursorManager>();
            if (FindObjectOfType<CustomScene>())
            {
                if (!cursorManager) gameObject.AddComponent<CursorManager>();
            }
            else if (cursorManager)
            {
                Destroy(cursorManager);
            }
        }

        private static void ApplyPatches()
        {
            if (FindObjectOfType<CustomScene>())
            {
                GameObject.Find("CustomScene/CamBase/Camera/CameraTarget")?.SetActive(!DisableCameraTarget.Value);
                GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsCharaName")?.SetActive(!DisableCharaName.Value);
            }
#if KK
            else if (FindObjectOfType<StudioScene>())
            {
                GameObject.Find("StudioScene/Camera/Main Camera/CameraTarget")?.SetActive(!DisableCameraTarget.Value);
            }
            else if (FindObjectOfType<HSceneProc>())
            {
                GameObject.Find("HScene/CameraBase/Camera/CameraTarget")?.SetActive(!DisableCameraTarget.Value);
            }
#endif
        }
    }
}
