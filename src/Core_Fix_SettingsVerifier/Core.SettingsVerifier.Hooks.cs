using Common;
using HarmonyLib;

namespace IllusionFixes
{
    public partial class SettingsVerifier
    {
        internal static partial class Hooks
        {
            /// <summary>
            /// Run the code for reading setup.xml when inside studio. Done in a Manager.Config.Start hook because the xmlRead method needs stuff to be initialized first.
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Config), "Start")]
            internal static void ManagerConfigStart()
            {
                if (CommonCode.InsideStudio)
                    ReadSetupXml();
            }
        }
    }
}
