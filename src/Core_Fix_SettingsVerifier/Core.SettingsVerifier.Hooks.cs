using System;
using BepInEx.Harmony;
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
                var h = HarmonyWrapper.PatchAll(typeof(Hooks));

#if KK
                // Only exists in KKP, corrupted file will cause white screen on boot
                var tutDatType = Type.GetType("TutorialData, Assembly-CSharp");
                if (tutDatType != null)
                {
                    var loadM = tutDatType.GetMethod("Load", AccessTools.all);
                    h.Patch(loadM, finalizer: new HarmonyMethod(typeof(Hooks), nameof(ExceptionEater)));
                }
#endif
            }

            /// <summary>
            /// Finalizer that logs and eats exceptions thrown by the patched method
            /// </summary>
            internal static Exception ExceptionEater(Exception __exception)
            {
                if (__exception != null) UnityEngine.Debug.LogError(__exception);
                return null;
            }

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
