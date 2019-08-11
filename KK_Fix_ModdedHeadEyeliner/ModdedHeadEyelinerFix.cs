using BepInEx;
using BepInEx.Harmony;
using Common;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace KK_Fix_ModdedHeadEyeliner
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class ModdedHeadEyelinerFix : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.headfix";
        public const string PluginName = "Head Fix";

        private void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            var harmony = HarmonyWrapper.PatchAll(typeof(ModdedHeadEyelinerFix));
            var getTextureMethod = typeof(ChaControl).GetMethod("GetTexture", AccessTools.all);
            if (getTextureMethod == null) throw new ArgumentException("Could not find ChaControl.GetTexture");
            if (getTextureMethod.GetParameters().Any(x => x.Name == "addStr"))
                harmony.Patch(typeof(ChaControl).GetMethod("GetTexture", AccessTools.all), null, new HarmonyMethod(typeof(ModdedHeadEyelinerFix).GetMethod(nameof(GetTexture), AccessTools.all)));
        }

        /// <summary>
        /// Strip the "addStr" and attempt to get the texture again. This fixes head types missing eyeliners if they don't have one defined for that specific head.
        /// </summary>
        public static void GetTexture(ChaListDefine.CategoryNo type, int id, ChaListDefine.KeyType assetBundleKey, ChaListDefine.KeyType assetKey, string addStr, ChaControl __instance, ref Texture2D __result)
        {
            if (__result == null && !addStr.IsNullOrEmpty())
                __result = Traverse.Create(__instance).Method("GetTexture", type, id, assetBundleKey, assetKey, "").GetValue() as Texture2D;
        }
    }
}
