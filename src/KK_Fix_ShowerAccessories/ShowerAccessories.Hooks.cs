using System;
using ActionGame;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;

namespace IllusionFixes
{
    public partial class ShowerAccessories
    {
        // Disable these when entering shower even if they are set as main accessories
        private static readonly List<AccessoryEntry> _accessoryBlacklist = new List<AccessoryEntry>
        {
            new AccessoryEntry(128, 0, "a_n_shoulder"),
            new AccessoryEntry(128, 1, "a_n_shoulder"),
            new AccessoryEntry(129, 0, "a_n_hand"),
            new AccessoryEntry(125, 0, "a_n_back"),
            new AccessoryEntry(125, 1, "a_n_back"),
            new AccessoryEntry(125, 2, "a_n_back"),
            new AccessoryEntry(125, 12, "a_n_back"),
        };

        private readonly struct AccessoryEntry
        {
            public readonly int Type;
            public readonly int Id;
            public readonly string ParentKey;

            public AccessoryEntry(int type, int id, string parentKey)
            {
                Type = type;
                Id = id;
                ParentKey = parentKey;
            }
        }

        private static void FixAccessoryState(ChaControl chaControl)
        {
            //Turn accessories back on and then turn off only Sub accessories
            chaControl.SetAccessoryStateAll(true);
            chaControl.SetAccessoryStateCategory(1, false);

            // Turn off backpacks that are set as main accessories by default
            for (var index = 0; index < chaControl.nowCoordinate.accessory.parts.Length; index++)
            {
                var acc = chaControl.nowCoordinate.accessory.parts[index];
                if (_accessoryBlacklist.Any(x => acc.type == x.Type && acc.id == x.Id && acc.parentKey.StartsWith(x.ParentKey)))
                    chaControl.fileStatus.showAccessory[index] = false;
            }
        }

        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
            internal static void HSceneShowerFix()
            {
                try
                {
                    var map = (ActionMap)AccessTools.Field(typeof(HSceneProc), "map").GetValue(Singleton<HSceneProc>.Instance);
                    if (map.no == 52) //shower
                    {
                        var lstFemale = (List<ChaControl>)AccessTools.Field(typeof(HSceneProc), "lstFemale")
                            .GetValue(Singleton<HSceneProc>.Instance);
                        var chaControl = lstFemale[0];

                        FixAccessoryState(chaControl);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(AI), "ArrivalSet")]
            internal static void OverworldShowerFix(AI __instance, ActionControl.ResultInfo result)
            {
                try
                {
                    if (result.actionNo == 2) //shower
                    {
                        var chaControl = Traverse.Create(__instance).Property("npc").Property<ChaControl>("chaCtrl").Value;
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
