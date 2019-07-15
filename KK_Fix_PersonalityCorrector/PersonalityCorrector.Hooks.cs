using ActionGame;
using Harmony;

namespace KK_Fix_PersonalityCorrector
{
    public partial class PersonalityCorrector
    {
        private class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.GetRandomFemaleCard))]
            public static void GetRandomFemaleCard(ref ChaFileControl[] __result)
            {
                foreach (var chaFileControl in __result)
                    CheckPersonalityAndOverride(chaFileControl);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PreviewClassData), nameof(PreviewClassData.Set), new[] { typeof(SaveData.CharaData) })]
            public static void SetClassChara(SaveData.CharaData charaData)
            {
                CheckPersonalityAndOverride(charaData.charFile);
            }
        }
    }
}
