using BepInEx;
using BepInEx.Logging;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    /// <summary>
    /// Changes any invalid personalities to the "Pure" personality to prevent the game from breaking when adding them to the class
    /// </summary>
    [BepInProcess(Constants.GameProcessName)]
#if KK
    [BepInProcess(Constants.GameProcessNameSteam)]
#endif
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
#if KK
            switch (chaFileControl.parameter.personality)
            {
                case 30: //0727 Free DLC
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/14.unity3d"))
                    {
                        ShowPersonalityMissingMessage(chaFileControl, "0727 Free DLC");
                        chaFileControl.parameter.personality = DefaultPersonality;
                        return true;
                    }
                    break;
                case 31: //0727 Paid DLC #1
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/15.unity3d"))
                    {
                        ShowPersonalityMissingMessage(chaFileControl, "0727 Summer Expansion");
                        chaFileControl.parameter.personality = DefaultPersonality;
                        return true;
                    }
                    break;
                case 32: //0727 Paid DLC #1
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/16.unity3d"))
                    {
                        ShowPersonalityMissingMessage(chaFileControl, "0727 Summer Expansion");
                        chaFileControl.parameter.personality = DefaultPersonality;
                        return true;
                    }
                    break;
                case 33: //0727 Paid DLC #1
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/17.unity3d"))
                    {
                        ShowPersonalityMissingMessage(chaFileControl, "0727 Summer Expansion");
                        chaFileControl.parameter.personality = DefaultPersonality;
                        return true;
                    }
                    break;
                case 34:
                case 35:
                case 36:
                case 37: //1221 Paid DLC #2
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/20.unity3d"))
                    {
                        ShowPersonalityMissingMessage(chaFileControl, "1221 AfterSchool Expansion");
                        chaFileControl.parameter.personality = DefaultPersonality;
                        return true;
                    }
                    break;
                case 38: //EmotionCreators preorder bonus personality
                    if (!AssetBundleCheck.IsFile("etcetra/list/config/50.unity3d"))
                    {
                        ShowPersonalityMissingMessage(chaFileControl, "EmotionCreators preorder bonus (yoyaku)");
                        chaFileControl.parameter.personality = DefaultPersonality;
                        return true;
                    }
                    break;
                case 80:
                case 81:
                case 82:
                case 83:
                case 84:
                case 85:
                case 86: //Story character personalities added by a mod
                    ShowPersonalityInvalidMessage(chaFileControl);
                    chaFileControl.parameter.personality = DefaultPersonality;
                    return true;
                default:
                    if (chaFileControl.parameter.personality > 38)
                    {
                        if (!Manager.Voice.Instance.voiceInfoDic.ContainsKey(chaFileControl.parameter.personality))
                        {
                            ShowPersonalityMissingMessage(chaFileControl);
                            chaFileControl.parameter.personality = DefaultPersonality;
                            return true;
                        }
                    }
                    break;
            }
#else
            if (!Manager.Voice.infoTable.ContainsKey(chaFileControl.parameter.personality))
            {
                ShowPersonalityMissingMessage(chaFileControl);
                chaFileControl.parameter.personality = DefaultPersonality;
                return true;
            }
#endif
            return false;
        }

        private static void ShowPersonalityInvalidMessage(ChaFileControl cf) =>
            Utilities.Logger.Log(LogLevel.Message | LogLevel.Warning,
                cf.parameter.fullname + " - Modded personality " +
                cf.parameter.personality + " is not compatible with story mode, resetting to Pure");

        private static void ShowPersonalityMissingMessage(ChaFileControl cf, string dlcName = null) =>
            Utilities.Logger.Log(LogLevel.Message | LogLevel.Warning,
                cf.parameter.fullname + " - Personality " +
                cf.parameter.personality + (dlcName != null ? " from " + dlcName : "") + " is missing, resetting to Pure");
    }
}
