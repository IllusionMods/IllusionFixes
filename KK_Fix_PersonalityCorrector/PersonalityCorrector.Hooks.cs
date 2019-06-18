using Harmony;
using System.Collections.Generic;

namespace KK_Fix_PersonalityCorrector
{
    internal class Hooks
    {
        public static int DefaultPersonality = 8; //Pure

        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.GetRandomFemaleCard))]
        public static void GetRandomFemaleCard(ref ChaFileControl[] __result)
        {
            foreach (ChaFileControl chaFileControl in __result)
                CheckPersonalityAndOverride(chaFileControl);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ActionGame.ClassRoomCharaFile), nameof(ActionGame.ClassRoomCharaFile.InitializeList))]
        public static void InitializeList(ActionGame.ClassRoomCharaFile __instance)
        {
            Dictionary<int, ChaFileControl> chaFileDic = Traverse.Create(__instance).Field("chaFileDic").GetValue<Dictionary<int, ChaFileControl>>();

            foreach (var x in chaFileDic)
                CheckPersonalityAndOverride(x.Value);
        }

        private static void CheckPersonalityAndOverride(ChaFileControl chaFileControl)
        {
            switch (chaFileControl.parameter.personality)
            {
                case 30: //0727 Free DLC
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/14.unity3d"))
                        chaFileControl.parameter.personality = DefaultPersonality;
                    break;
                case 31: //0727 Paid DLC #1
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/15.unity3d"))
                        chaFileControl.parameter.personality = DefaultPersonality;
                    break;
                case 32: //0727 Paid DLC #1
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/16.unity3d"))
                        chaFileControl.parameter.personality = DefaultPersonality;
                    break;
                case 33: //0727 Paid DLC #1
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/17.unity3d"))
                        chaFileControl.parameter.personality = DefaultPersonality;
                    break;
                case 34:
                case 35:
                case 36:
                case 37: //1221 Paid DLC #2
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/20.unity3d"))
                        chaFileControl.parameter.personality = DefaultPersonality;
                    break;
                case 38: //EmotionCreators preorder bonus personality
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/50.unity3d"))
                        chaFileControl.parameter.personality = DefaultPersonality;
                    break;
                case 80:
                case 81:
                case 82:
                case 83:
                case 84:
                case 85:
                case 86: //Story character personalities added by a mod
                    chaFileControl.parameter.personality = DefaultPersonality;
                    break;
            }
        }
    }
}
