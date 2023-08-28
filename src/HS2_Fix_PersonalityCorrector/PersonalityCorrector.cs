using System;
using System.Diagnostics;
using System.IO;
using AIChara;
using BepInEx;
using BepInEx.Logging;
using Common;
using HarmonyLib;
using Manager;
using System.Linq;

namespace IllusionFixes
{
    /// <summary>
    /// Changes any invalid personalities in character cards to a valid personality to prevent the game from breaking
    /// </summary>
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class PersonalityCorrector : BaseUnityPlugin
    {
        public const string GUID = "HS2_Fix_PersonalityCorrector";
        public const string PluginName = "Personality Corrector";

        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaFileParameter), nameof(ChaFileParameter.personality), MethodType.Setter)]
            [HarmonyPatch(typeof(ChaFileParameter), nameof(ChaFileParameter2.personality), MethodType.Setter)]
            [HarmonyPriority(Priority.First)]
            private static void MissingPersonalityFix(ChaFileParameter __instance, ref int value)
            {
                //Utilities.Logger.Log(LogLevel.Warning | LogLevel.Message, $"[{__instance.fullname ?? "???"}] uses personality [{value}]");

                if (value == 0)
                    return;

                var personalityTable = Game.infoPersonalParameterTable;
                if (personalityTable.ContainsKey(value))
                    return;

                if (personalityTable.Count == 0)
                {
                    Utilities.Logger.LogError("Game.infoPersonalParameterTable is empty???\n" + new StackTrace());
                    return;
                }

                var minKey = personalityTable.Keys.Min();
                Utilities.Logger.Log(LogLevel.Warning | LogLevel.Message, $"The character [{__instance?.fullname ?? "???"}] uses personality [{value}] which doesn't exist in your game. " +
                                                                          $"Check your game files and make sure you have all updates and expansions installed. " +
                                                                          $"The character's personality will be overriden to [{minKey}] to prevent a game crash.");
                value = minKey;
            }
        }
    }
}
