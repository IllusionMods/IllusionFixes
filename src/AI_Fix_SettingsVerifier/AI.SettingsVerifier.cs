using System;
using System.Linq;
using BepInEx;
using Common;
using HarmonyLib;
using Manager;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class SettingsVerifier : BaseUnityPlugin
    {
        public const string GUID = "AI_Fix_SettingsVerifier";

        internal static partial class Hooks
        {
            /// <summary>
            /// Check if the loaded language is supported, if not reset to a sane value
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(GameSystem), "LoadLanguage")]
            internal static void CheckLanguage(GameSystem __instance)
            {
                if (!Enum.GetValues(typeof(GameSystem.Language)).Cast<GameSystem.Language>().Contains(__instance.language))
                {
                    UnityEngine.Debug.LogWarning("Unsupported language was set, resetting to Japanese");
                    Traverse.Create(__instance).Property(nameof(__instance.language)).SetValue(GameSystem.Language.Japanese);
                }
            }
        }
    }
}
