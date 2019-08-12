using ActionGame;
using HarmonyLib;
using System.Collections.Generic;

namespace IllusionFixes
{
    public partial class ShowerAccessories
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
            public static void MapSameObjectDisable(HSceneProc __instance)
            {
                var map = (ActionMap)AccessTools.Field(typeof(HSceneProc), "map").GetValue(Singleton<HSceneProc>.Instance);
                if (map.no == 52) //shower
                {
                    var lstFemale = (List<ChaControl>)AccessTools.Field(typeof(HSceneProc), "lstFemale").GetValue(Singleton<HSceneProc>.Instance);

                    //Turn accessories back on and then turn off only Sub accessories
                    lstFemale[0].SetAccessoryStateAll(true);
                    lstFemale[0].SetAccessoryStateCategory(1, false);
                }
            }
        }
    }
}
