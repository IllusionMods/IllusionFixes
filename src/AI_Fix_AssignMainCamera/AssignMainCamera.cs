using Studio;
using BepInEx;
using HarmonyLib;
using Common;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class AssignMainCamera : BaseUnityPlugin
    {
        public const string GUID = "kky.ai.assignmaincamera";
        public const string PluginName = "AI Assign Main Camera Fix";

        private void Awake()
        {
            var harmony = new Harmony(nameof(AssignMainCamera));
            harmony.PatchAll(typeof(AssignMainCamera));
        }

        [HarmonyPatch(typeof(Studio.CameraControl), "Awake")]
        [HarmonyPostfix]
        public static void setMainCamera(Studio.CameraControl __instance)
        {
            if (__instance.mainCmaera == null) __instance.mainCmaera = Camera.main;
        }
    }
}
