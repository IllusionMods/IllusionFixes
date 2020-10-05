using BepInEx;
using BepInEx.Configuration;
using Common;
using HarmonyLib;
using System.IO;
using UploaderSystem;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class DownloadRenamer : BaseUnityPlugin
    {
        public const string GUID = "EC_Fix_DownloadRenamer";
        public const string PluginName = "Download Renamer";
        public static ConfigEntry<bool> EnambleRenaming { get; private set; }

        internal void Start()
        {
            Harmony.CreateAndPatchAll(typeof(DownloadRenamer));
            EnambleRenaming = Config.Bind(Utilities.ConfigSectionTweaks, "Rename downloads", true,
                new ConfigDescription("When enabled, maps, scenes, poses, and characters downloaded in game will have their file names changed to match the ones on the Illusion website."));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(NetUIControl), "SaveDownloadFile")]
        public static bool SaveDownloadFilePrefix(NetUIControl __instance, byte[] bytes, NetworkInfo.BaseIndex info)
        {
            if (!EnambleRenaming.Value) return true;

            var fileName = GetFilename(__instance.dataType, info);

            if (string.IsNullOrEmpty(fileName))
            {
                // Unknown type, fallback to the original function
                return true;
            }

            File.WriteAllBytes(fileName, bytes);

            return false;
        }

        /// <summary>
        /// Generate names to match the web downloader, which are more sensible.
        /// The original names reflect the time of download, which is not only redundant
        /// information, but also allows redundant copies to be downloaded.
        /// </summary>
        private static string GetFilename(int dataType, NetworkInfo.BaseIndex info)
        {
            switch (dataType)
            {
                case 0:
                    var charaInfo = info as NetworkInfo.CharaInfo;
                    return GetFileName(charaInfo?.sex == 1 ? "chara/female" : "chara/male", "emocre_chara_", info.idx);
                case 1:
                    return GetFileName("map/data", "emocre_map_", info.idx);
                case 2:
                    return GetFileName("pose/data", "emocre_pose_", info.idx);
                case 3:
                    return GetFileName("edit/scene", "emocre_scene_", info.idx);
                default:
                    Utilities.Logger.LogWarning($"Unknown download file type {dataType}, can't rename");
                    return null;
            }
        }

        private static string GetFileName(string dir, string prefix, int index) => UserData.Create(dir) + prefix + index.ToString("D7") + ".png";
    }
}
