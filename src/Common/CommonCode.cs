using UnityEngine;

namespace Common
{
    internal static class CommonCode
    {
        internal static bool InsideStudio => Application.productName == "CharaStudio" || Application.productName == "StudioNEOV2";
        internal static bool InsideKoikatsuParty => Application.productName == "Koikatsu Party";
    }
}
