using UnityEngine;

namespace Common
{
    internal static class Constants
    {
        /// <summary>
        /// Version number used by all plugins. Must be convertible to <see cref="System.Version"/>
        /// Uses ReleaseVersion.HotfixVersion format.
        /// </summary>
        public const string PluginsVersion = "18.0.1";

#if AI
        internal static bool InsideStudio => Application.productName == StudioProcessName;
        internal const string StudioProcessName = "StudioNEOV2";
        internal const string GameProcessName = "AI-Syoujyo";
#elif EC
        internal static bool InsideStudio => false;
        internal const string GameProcessName = "EmotionCreators";
#elif HS
        internal static bool InsideStudio => Application.productName == StudioProcessName || Application.productName == StudioProcessName32bit;
        internal const string StudioProcessName = "StudioNEO_64";
        internal const string StudioProcessName32bit = "StudioNEO_32";
        internal const string GameProcessName = "HoneySelect_64";
        internal const string GameProcessName32bit = "HoneySelect_32";
        internal const string BattleArenaProcessName = "BattleArena_64";
        internal const string BattleArenaProcessName32bit = "BattleArena_32";
#elif HS2
        internal static bool InsideStudio => Application.productName == StudioProcessName;
        internal const string StudioProcessName = "StudioNEOV2";
        internal const string GameProcessName = "HoneySelect2";
        internal const string VRProcessName = "HoneySelect2VR";
#elif KK
        internal static bool InsideStudio => Application.productName == StudioProcessName;
        internal const string StudioProcessName = "CharaStudio";
        internal const string GameProcessName = "Koikatu";
        internal const string GameProcessNameSteam = "Koikatsu Party";
        internal const string VRProcessName = "KoikatuVR";
        internal const string VRProcessNameSteam = "Koikatsu Party VR";
#elif PH
        internal static bool InsideStudio => Application.productName == StudioProcessName || Application.productName == StudioProcessName32bit;
        internal const string GameProcessName = "PlayHome64bit";
        internal const string GameProcessName32bit = "PlayHome32bit";
        internal const string StudioProcessName = "PlayHomeStudio64bit";
        internal const string StudioProcessName32bit = "PlayHomeStudio32bit";
#elif KKS
        internal static bool InsideStudio => Application.productName == StudioProcessName;
        internal const string GameProcessName = "KoikatsuSunshine";
        internal const string StudioProcessName = "CharaStudio";
        internal const string VRProcessName = "KoikatsuSunshine_VR";
#endif
    }
}
