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
        public const string GUID = "HS2_Fix_SettingsVerifier";

        internal static partial class Hooks
        {
            /// <summary>
            /// Check if the loaded language is supported, if not reset to a sane value
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(GameSystem), "LoadLanguage")]
            internal static void CheckLanguage(GameSystem __instance)
            {
                if (__instance.language != GameSystem.Language.Japanese && !IsSteam())
                {
                    UnityEngine.Debug.LogWarning("Unsupported language was set, resetting to Japanese");
                    Traverse.Create(__instance).Property(nameof(__instance.language)).SetValue(GameSystem.Language.Japanese);
                }
            }

            private static bool IsSteam()
            {
                // Checking GameSystem.Language is no longer working in HS2 since jp version has all of the cultures listed despite not actually supporting them
                return typeof(AIChara.ChaFileDefine).GetMethod("GetOriginalValueFromSteam", new[] { typeof(float), typeof(int) }) != null;
            }
        }
    }
}
