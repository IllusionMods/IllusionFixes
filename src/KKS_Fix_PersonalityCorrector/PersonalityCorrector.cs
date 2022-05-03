using BepInEx;
using BepInEx.Logging;
using Common;
using HarmonyLib;
using Manager;

namespace IllusionFixes
{
    /// <summary>
    /// Changes any invalid personalities to the "Pure" personality to prevent the game from breaking when adding them to the class
    /// </summary>
    [BepInProcess(Constants.GameProcessName)]
    //[BepInProcess(Constants.GameProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class PersonalityCorrector : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_PersonalityCorrector";
        public const string PluginName = "Personality Corrector";

        public static int DefaultPersonality = 8; // 8 - Pure

        internal void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        public static bool CheckPersonalityAndOverride(ChaFileControl chaFileControl)
        {
            if (!Voice.infoTable.ContainsKey(chaFileControl.parameter.personality))
            {
                ShowPersonalityMissingMessage(chaFileControl);
                chaFileControl.parameter.personality = DefaultPersonality;
                return true;
            }

            return false;
        }

        private static void ShowPersonalityInvalidMessage(ChaFileControl cf) =>
            Utilities.Logger.Log(LogLevel.Message | LogLevel.Warning,
                cf.parameter.fullname + " - Modded personality " +
                cf.parameter.personality + " is not compatible with story mode, resetting to Pure");

        private static void ShowPersonalityMissingMessage(ChaFileControl cf) =>
            Utilities.Logger.Log(LogLevel.Message | LogLevel.Warning,
                cf.parameter.fullname + " - Personality " +
                cf.parameter.personality + " is missing, resetting to Pure");
    }
}
