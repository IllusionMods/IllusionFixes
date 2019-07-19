using BepInEx;
using Common;
using Harmony;
using System.Linq;
using UnityEngine;

namespace KK_Fix_HeadFix
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class HeadFix : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.headfix";
        public const string PluginName = "Head Fix";

        private void Awake()
        {
            var harmony = HarmonyInstance.Create(GUID);
            var getTextureMethod = typeof(ChaControl).GetMethod("GetTexture", AccessTools.all);
            if (getTextureMethod.GetParameters().Any(x => x.Name == "addStr"))
                harmony.Patch(typeof(ChaControl).GetMethod("GetTexture", AccessTools.all), null, new HarmonyMethod(typeof(HeadFix).GetMethod(nameof(GetTexture), AccessTools.all)));
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
