using BepInEx;
using BepInEx.Logging;
using Common;
using Harmony;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = BepInEx.Logger;

namespace KK_Fix_MakerOptimizations
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class MakerOptimizations : BaseUnityPlugin
    {
        public const string GUID = "keelhauled.fixcompilation";
        public const string PluginName = "Maker Optimization";

        [DisplayName("Disable \"NEW\" indicator animation")]
        [Description("Major performance improvement in maker if there are many new items. Almost no visual effect.")]
        public static ConfigWrapper<bool> DisableNewAnimation { get; private set; }

        [DisplayName("Disable \"NEW\" indicator for new items")]
        [Description("Good performance improvement in maker if there are many new items.\n\nChanges take effect after maker restart.")]
        public static ConfigWrapper<bool> DisableNewIndicator { get; private set; }

        [DisplayName("Disable maker IK")]
        [Description("Improves performance and reduces stuttering at the cost of not recalculating positions of some body parts.\n\n" +
                     "Most noticeable on characters with wide hips where the hands are not moving with the hip line.\n\n" +
                     "Warning: This setting will get reset to false if Stiletto is installed to avoid issues!\n\n" +
                     "Changes take effect after game restart.")]
        public static ConfigWrapper<bool> DisableIKCalc { get; private set; }

        [DisplayName("Disable camera target (white focus ring)")]
        [Description("Warning: This setting overrides any game setting that enables the ring.")]
        public static ConfigWrapper<bool> DisableCameraTarget { get; private set; }

        [DisplayName("Disable character name box in maker")]
        [Description("Hides the name box in the bottom right part of the maker, giving you a clearer look at the character.")]
        public static ConfigWrapper<bool> DisableCharaName { get; private set; }

        [DisplayName("Deactivate hidden tabs in maker")]
        [Description("Major performance improvement at the cost of slower switching between tabs in maker.\n\nChanges take effect after maker restart.")]
        public static ConfigWrapper<bool> DisableHiddenTabs { get; private set; }

        [DisplayName("Manage cursor in maker")]
        [Description("Lock and hide the cursor when moving the camera in maker.")]
        public static ConfigWrapper<bool> ManageCursor { get; private set; }

        public MakerOptimizations()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            DisableNewAnimation = new ConfigWrapper<bool>("DisableNewAnimation", this, true);
            DisableNewIndicator = new ConfigWrapper<bool>("DisableNewIndicator", this, true);
            DisableIKCalc = new ConfigWrapper<bool>("DisableIKCalc", this, true);
            DisableCameraTarget = new ConfigWrapper<bool>("DisableCameraTarget", this, false);
            DisableCharaName = new ConfigWrapper<bool>("DisableCharaName", this, true);
            DisableHiddenTabs = new ConfigWrapper<bool>("DisableHiddenTabs", this, SystemInfo.processorFrequency < 2700);
            ManageCursor = new ConfigWrapper<bool>("ManageCursor", this, true);
        }

        protected void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            SceneManager.sceneLoaded += SceneLoaded;
            DisableCameraTarget.SettingChanged += (sender, args) => ApplyPatches();
            DisableCharaName.SettingChanged += (sender, args) => ApplyPatches();

            Hooks.Patch(HarmonyInstance.Create(GUID));
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
