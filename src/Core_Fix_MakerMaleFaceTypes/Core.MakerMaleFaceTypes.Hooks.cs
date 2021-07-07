using CharaCustom;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace IllusionFixes
{
    public partial class MakerMaleFaceTypes
    {
        internal static class Hooks
        {
            internal static void InstallHooks()
            {
                var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsSelectWindow), "Start")]
            public static void CvsSelectWindowStart(CvsSelectWindow __instance)
            {
                Instance.StartCoroutine(OnMakerLoadingCo());
            }

            private static IEnumerator OnMakerLoadingCo()
            {
                // Follow KKAPI methodology here
                for (int i = 0; i < 7; i++)
                    yield return null;

                Instance.MakerFinishedLoading();

            }
        }

    }
}
