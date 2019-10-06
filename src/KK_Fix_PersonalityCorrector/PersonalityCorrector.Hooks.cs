using ActionGame;
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
        }
    }
}
