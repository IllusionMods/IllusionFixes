using Common;
using HarmonyLib;

namespace IllusionFixes
{
    public partial class SettingsVerifier
    {
        internal static partial class Hooks
        {
            public static void Apply()
            {
                Harmony.CreateAndPatchAll(typeof(Hooks));
            }

            /// <summary>
            /// Run the code for reading setup.xml when inside studio. Done in a Manager.Config.Start hook because the xmlRead method needs stuff to be initialized first.
            /// </summary>
#if HS2 || KKS
            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Config), MethodType.StaticConstructor)]
#else
            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Config), nameof(Manager.Config.Start))]
#endif
            internal static void ManagerConfigStart()
            {
                if (Utilities.InsideStudio)
                    ReadSetupXml();
            }
        }
    }
}
