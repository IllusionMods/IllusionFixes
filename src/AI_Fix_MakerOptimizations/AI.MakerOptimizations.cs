using BepInEx;
using BepInEx.Configuration;
using CharaCustom;
using Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class MakerOptimizations : BaseUnityPlugin
    {
        public const string GUID = "AI_Fix_MakerOptimizations";
        public const string PluginName = "Maker Optimizations";

        public static ConfigEntry<bool> DisableCameraTarget { get; private set; }
        public static ConfigEntry<bool> ManageCursor { get; private set; }

        internal void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins())
                return;

            DisableCameraTarget = Config.Bind(Utilities.ConfigSectionTweaks, "Disable camera target (white focus ring)", false, new ConfigDescription("Warning: This setting overrides any game setting that enables the ring."));
            ManageCursor = Config.Bind(Utilities.ConfigSectionTweaks, "Manage cursor in maker", true, new ConfigDescription("Lock and hide the cursor when moving the camera in maker."));

            SceneManager.sceneLoaded += SceneLoaded;
            DisableCameraTarget.SettingChanged += (sender, args) => ApplyPatches();
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyPatches();

            var cursorManager = gameObject.GetComponent<CursorManager>();

            if (FindObjectOfType<CustomBase>())
            {
                if (!cursorManager)
                    gameObject.AddComponent<CursorManager>();
            }
            else if (cursorManager)
            {
                Destroy(cursorManager);
            }
        }

        private static void ApplyPatches()
        {
            if (FindObjectOfType<CustomBase>())
                GameObject.Find("CharaCustom/CustomControl/CharaCamera/Main Camera/CameraTarget")?.SetActive(!DisableCameraTarget.Value);
        }
    }
}
