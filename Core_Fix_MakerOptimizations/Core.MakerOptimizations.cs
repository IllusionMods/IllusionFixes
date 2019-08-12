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

        public static ConfigWrapper<bool> DisableNewAnimation { get; private set; }
        public static ConfigWrapper<bool> DisableNewIndicator { get; private set; }
        public static ConfigWrapper<bool> DisableIKCalc { get; private set; }
        public static ConfigWrapper<bool> DisableCameraTarget { get; private set; }
        public static ConfigWrapper<bool> DisableCharaName { get; private set; }
        public static ConfigWrapper<bool> DisableHiddenTabs { get; private set; }
        public static ConfigWrapper<bool> ManageCursor { get; private set; }

        public MakerOptimizations()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            DisableNewAnimation = Utilities.FixesConfig.Wrap(Utilities.ConfigSectionTweaks, "Disable NEW indicator animation", "Good performance improvement in maker if there are many new items.\nChanges take effect after maker restart.", true);
            DisableNewIndicator = Utilities.FixesConfig.Wrap(Utilities.ConfigSectionTweaks, "Disable NEW indicator for new items", "visual glitches like 2 coordinates loaded at once.", true);
            DisableIKCalc = Utilities.FixesConfig.Wrap(Utilities.ConfigSectionTweaks, "Disable maker IK", "Improves performance and reduces stuttering at the cost of not recalculating positions of some body parts.\nMost noticeable on characters with wide hips where the hands are not moving with the hip line.\nWarning: This setting will get reset to false if Stiletto is installed to avoid issues!\nChanges take effect after game restart.", true);
            DisableCameraTarget = Utilities.FixesConfig.Wrap(Utilities.ConfigSectionTweaks, "Disable camera target (white focus ring)", "Warning: This setting overrides any game setting that enables the ring.", false);
            DisableCharaName = Utilities.FixesConfig.Wrap(Utilities.ConfigSectionTweaks, "Disable character name box in maker", "Hides the name box in the bottom right part of the maker, giving you a clearer look at the character.", true);
            DisableHiddenTabs = Utilities.FixesConfig.Wrap(Utilities.ConfigSectionTweaks, "Deactivate hidden tabs in maker", "Major performance improvement at the cost of slower switching between tabs in maker.\nChanges take effect after maker restart.", SystemInfo.processorFrequency < 2700);
            ManageCursor = Utilities.FixesConfig.Wrap(Utilities.ConfigSectionTweaks, "Manage cursor in maker", "Lock and hide the cursor when moving the camera in maker.", true);
        }

        protected void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            SceneManager.sceneLoaded += SceneLoaded;
            DisableCameraTarget.SettingChanged += (sender, args) => ApplyPatches();
            DisableCharaName.SettingChanged += (sender, args) => ApplyPatches();

            Hooks.InstallHooks();
        }

        private void Start()
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
