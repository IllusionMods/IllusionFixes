using BepInEx;
using BepInEx.Configuration;
using CharaCustom;
using Common;
using UnityEngine.SceneManagement;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class MakerOptimizations : BaseUnityPlugin
    {
        public const string GUID = "AI_Fix_MakerOptimizations";
        public const string PluginName = "Maker Optimizations";

        public static ConfigEntry<bool> ManageCursor { get; private set; }

        internal void Awake()
        {
            ManageCursor = Config.Bind(Utilities.ConfigSectionTweaks, "Manage cursor in maker", true, new ConfigDescription("Lock and hide the cursor when moving the camera in maker."));

            SceneManager.sceneLoaded += SceneLoaded;
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
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
    }
}
