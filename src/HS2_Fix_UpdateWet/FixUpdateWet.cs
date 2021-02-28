using AIChara;
using BepInEx;
using Common;
using HarmonyLib;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class FixUpdateWet : BaseUnityPlugin
    {
        public const string GUID = "HS2_Fix_UpdateWet";
        public const string PluginName = "Fix UpdateWet Exceptions";

        private static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            Harmony.CreateAndPatchAll(typeof(FixUpdateWet), GUID);
        }

        /// <summary>
        /// Some modded items have null renderers in the CmpHair or CmpClothes MB setup
        /// This is harmless everywhere but throws an error in ChaControl.UpdateWet which causes wet effects to not apply properly and a major logging
        /// driven performance hit
        ///
        /// This fixes it by simply removing the non-existent renderer from the array which *I think* is entirely safe...doesn't seem to cause an issue
        /// I mean, the renderer isn't there anyway...hence it being null ;)
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "UpdateWet")]
        private static void UpdateWetPreHook(ChaControl __instance)
        {
            // Cleanup broken renderers
            foreach (CmpHair hair in __instance.cmpHair)
            {
                if (hair == null) continue;

                if (ContainsNulls(hair.rendHair)) hair.rendHair = hair.rendHair.RemoveNulls();
                if (ContainsNulls(hair.rendAccessory)) hair.rendAccessory = hair.rendAccessory.RemoveNulls();
            }
            foreach (CmpClothes clothes in __instance.cmpClothes)
            {
                if (clothes == null) continue;

                if (ContainsNulls(clothes.rendNormal01)) clothes.rendNormal01 = clothes.rendNormal01.RemoveNulls();
                if (ContainsNulls(clothes.rendNormal02)) clothes.rendNormal02 = clothes.rendNormal02.RemoveNulls();
                if (ContainsNulls(clothes.rendNormal03)) clothes.rendNormal03 = clothes.rendNormal03.RemoveNulls();
            }
        }

        private static bool ContainsNulls(Renderer[] renderers)
        {
            var containsNulls = renderers != null && renderers.Contains(null);
            if (containsNulls)
            {
                Logger.LogWarning("Found null renderers in the CmpHair or CmpClothes MBs. " +
                                  "This is most likely an issue with the last modded clothes you tried to use. " +
                                  "The nulls will be removed but the mod might have issues anyways and should be fixed by the author.");
            }
            return containsNulls;
        }
    }
}
