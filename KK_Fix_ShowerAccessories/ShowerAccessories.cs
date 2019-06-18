using ActionGame;
using BepInEx;
using Common;
using Harmony;
using System.Collections.Generic;

namespace KK_Fix_ShowerAccessories
{
    /// <summary>
    /// Prevents accessories from being disabled in the shower peeping mode
    /// </summary>
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class ShowerAccessories : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ShowerAccessories";
        public const string PluginName = "Shower Accessories Fix";

        private void Main() => HarmonyInstance.Create(GUID).PatchAll(typeof(ShowerAccessories));

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
