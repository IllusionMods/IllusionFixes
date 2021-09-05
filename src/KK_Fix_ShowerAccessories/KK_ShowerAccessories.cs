using System;
using ActionGame;
using ActionGame.Chara;
using BepInEx;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    // Code is in the shared project part
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    public partial class ShowerAccessories : BaseUnityPlugin
    {
        private static class Hooks
        {
            public static void ApplyHooks(string guid)
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), guid);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
            internal static void HSceneShowerFix()
            {
                try
                {
                    var map = Singleton<HSceneProc>.Instance.map;
                    if (map.no == 52) //shower
                    {
                        var lstFemale = Singleton<HSceneProc>.Instance.lstFemale;
                        FixAccessoryState(lstFemale[0]);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(AI), "ArrivalSet")]
            internal static void OverworldShowerFix(AI __instance, ActionControl.ResultInfo result)
            {
                try
                {
                    if (result.actionNo == 2) //shower
                    {
                        var chaControl = __instance.npc.chaCtrl;
                        FixAccessoryState(chaControl);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }
    }
}