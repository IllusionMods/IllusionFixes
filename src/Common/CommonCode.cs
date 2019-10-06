using UnityEngine;

namespace Common
{
    internal static class CommonCode
    {
        internal static bool InsideStudio => Application.productName == "CharaStudio";
        internal static bool InsideKoikatsuParty => Application.productName == "Koikatsu Party";
    }
}
