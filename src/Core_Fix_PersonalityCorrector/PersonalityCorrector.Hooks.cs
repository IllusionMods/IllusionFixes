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
            internal static void GetRandomFemaleCardPersonalityCheck(ref ChaFileControl[] __result)
            {
                foreach (var chaFileControl in __result)
                    CheckPersonalityAndOverride(chaFileControl);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PreviewClassData), nameof(PreviewClassData.Set), typeof(SaveData.CharaData))]
            internal static void SetClassCharaPersonalityCheck(SaveData.CharaData charaData)
            {
                CheckPersonalityAndOverride(charaData.charFile);
            }

#if KKS
            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveData.WorldData), nameof(SaveData.WorldData.SetBytes))]
            private static void LoadSavedataPersonalityCheck(SaveData.WorldData saveData)
            {
                foreach (var heroine in saveData.heroineList)
                    CheckPersonalityAndOverride(heroine.charFile);
            }
#elif KK
            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveData), nameof(SaveData.Load), typeof(string), typeof(string))]
            private static void LoadSavedataPersonalityCheck(SaveData __instance)
            {
                foreach (var heroine in __instance.heroineList)
                    CheckPersonalityAndOverride(heroine.charFile);
            }
#endif

            [HarmonyFinalizer]
#if KKS
            [HarmonyPatch(typeof(SaveData.WorldData), nameof(SaveData.WorldData.GetCallName), typeof(SaveData.CharaData), typeof(int))]
#elif KK
            [HarmonyPatch(typeof(SaveData), nameof(SaveData.GetCallName), typeof(SaveData.CharaData), typeof(int))]
#endif
            private static Exception CatchGetCallNameCrash(Exception __exception, SaveData.CharaData charaData, int id)
            {
                if (__exception != null)
                {
                    if (!CheckPersonalityAndOverride(charaData.charFile))
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
