using System;
using ActionGame;
using BepInEx.Logging;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    public partial class PersonalityCorrector
    {
        internal class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.GetRandomFemaleCard))]
            internal static void GetRandomFemaleCard(ref ChaFileControl[] __result)
            {
                foreach (var chaFileControl in __result)
                    CheckPersonalityAndOverride(chaFileControl);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PreviewClassData), nameof(PreviewClassData.Set), new[] { typeof(SaveData.CharaData) })]
            internal static void SetClassChara(SaveData.CharaData charaData) => CheckPersonalityAndOverride(charaData.charFile);
            

            [HarmonyFinalizer]
            [HarmonyPatch(typeof(SaveData), nameof(SaveData.GetCallName), typeof(SaveData.CharaData), typeof(int))]
            private static Exception CatchGetCallNameCrash(Exception __exception, SaveData.CharaData charaData, int id)
            {
                if (__exception != null)
                {
                    if(!CheckPersonalityAndOverride(charaData.charFile))
                    {
                        // If the personality does exist, the id will get auto reset even if we don't do anything
                        Utilities.Logger.Log(LogLevel.Warning, $"Failed to get CallName for personality={charaData.personality} id={id}. Resetting it to default.");
                        Utilities.Logger.LogWarning(__exception);
                    }
                }

                return null;
            }
        }
    }
}
