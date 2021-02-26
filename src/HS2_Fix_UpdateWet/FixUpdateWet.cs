using AIChara;
using BepInEx;
using BepInEx.Logging;
using Common;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class FixUpdateWet : BaseUnityPlugin
    {
        public const string GUID = "HS2_Fix_UpdateWet";
        public const string PluginName = "Fix UpdateWet Exceptions";

        private static FixUpdateWet Instance;

        private ManualLogSource Log => Logger;

        public void Start()
        {
            Instance = this;
            PatchMe();
        }

        private static void PatchMe()
        {
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(FixUpdateWet));
        }

        // Some modded items have null renderers in the CmpHair or CmpClothes MB setup
        // This is harmless everywhere but throws an error in ChaControl.UpdateWet which causes wet effects to not apply properly and a major logging
        // driven performance hit

        // This fixes it by simply removing the non-existent renderer from the array which *I think* is entirely safe...doesn't seem to cause an issue
        // I mean, the renderer isn't there anyway...hence it being null ;)

        [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "UpdateWet")]
        static void UpdateWetPreHook(ChaControl __instance)
        {
            // Cleanup broken renderers
            foreach (CmpHair hair in __instance.cmpHair)
            {         
                if (hair != null && hair.rendHair != null && hair.rendHair.Contains(null))
                {
                    hair.rendHair = hair.rendHair.Where(r => r != null).ToArray();
                }
                if (hair != null && hair.rendAccessory != null && hair.rendAccessory.Contains(null))
                {
                    hair.rendAccessory = hair.rendAccessory.Where(r => r != null).ToArray();
                }
            }
            foreach (CmpClothes clothes in __instance.cmpClothes)
            {
                if (clothes != null && clothes.rendNormal01 != null && clothes.rendNormal01.Contains(null))
                {
                    clothes.rendNormal01 = clothes.rendNormal01.Where(r => r != null).ToArray();
                }
                if (clothes != null && clothes.rendNormal02 != null && clothes.rendNormal02.Contains(null))
                {
                    clothes.rendNormal02 = clothes.rendNormal02.Where(r => r != null).ToArray();
                }
                if (clothes != null && clothes.rendNormal03 != null && clothes.rendNormal03.Contains(null))
                {
                    clothes.rendNormal03 = clothes.rendNormal03.Where(r => r != null).ToArray();
                }
            }
        }
    }
}
