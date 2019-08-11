using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Common;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KK_Fix_MakerOptimizations
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class MakerOptimizations : BaseUnityPlugin
    {
        public const string GUID = "keelhauled.fixcompilation";
        public const string PluginName = "Maker Optimization";

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

            DisableNewAnimation = Config.Wrap("Config", "Disable NEW indicator animation", "Good performance improvement in maker if there are many new items.\n\nChanges take effect after maker restart.", true);
            DisableNewIndicator = Config.Wrap("Config", "Disable NEW indicator for new items", "visual glitches like 2 coordinates loaded at once.", true);
            DisableIKCalc = Config.Wrap("Config", "Disable maker IK", "Improves performance and reduces stuttering at the cost of not recalculating positions of some body parts.\n\nMost noticeable on characters with wide hips where the hands are not moving with the hip line.\n\nWarning: This setting will get reset to false if Stiletto is installed to avoid issues!\n\nChanges take effect after game restart.", true);
            DisableCameraTarget = Config.Wrap("Config", "Disable camera target (white focus ring)", "Warning: This setting overrides any game setting that enables the ring.", false);
            DisableCharaName = Config.Wrap("Config", "Disable character name box in maker", "Hides the name box in the bottom right part of the maker, giving you a clearer look at the character.", true);
            DisableHiddenTabs = Config.Wrap("Config", "Deactivate hidden tabs in maker", "Major performance improvement at the cost of slower switching between tabs in maker.\n\nChanges take effect after maker restart.", SystemInfo.processorFrequency < 2700);
            ManageCursor = Config.Wrap("Config", "Manage cursor in maker", "Lock and hide the cursor when moving the camera in maker.", true);
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
                Logger.Log(LogLevel.Warning | LogLevel.Message, "Stiletto detected, disabling the DisableIKCalc optimization for compatibility");
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
            if (FindObjectOfType<StudioScene>())
            {
                GameObject.Find("StudioScene/Camera/Main Camera/CameraTarget")?.SetActive(!DisableCameraTarget.Value);
            }
            else if (FindObjectOfType<CustomScene>())
            {
                GameObject.Find("CustomScene/CamBase/Camera/CameraTarget")?.SetActive(!DisableCameraTarget.Value);
                GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsCharaName")?.SetActive(!DisableCharaName.Value);
            }
            else if (FindObjectOfType<HSceneProc>())
            {
                GameObject.Find("HScene/CameraBase/Camera/CameraTarget")?.SetActive(!DisableCameraTarget.Value);
            }
        }
    }
}
