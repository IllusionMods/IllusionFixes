using BepInEx;
using BepInEx.Logging;
using Common;
using HarmonyLib;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class CameraMaskResizeFix : BaseUnityPlugin
    {
        public const string GUID = "Fix_CameraMaskResize";
        public const string PluginName = "Fix camera color mask not resizing";
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
        }

        internal static class Hooks
        {
            private static int _lastWidth;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CameraEffectorColorMask), nameof(CameraEffectorColorMask.Update))]
            internal static void LoadCharaFbxDataAsync(CameraEffectorColorMask __instance)
            {
                var width = Screen.width;
                if (_lastWidth != width)
                {
                    if (_lastWidth != 0) // First run assume the size is correct
                    {
                        Logger.LogDebug($"Adjusting CameraEffectorColorMask targetTexture size because screen width changed from {_lastWidth} to {width}");
                        var newRt = new RenderTexture(width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                        Destroy(__instance.myCamera.targetTexture);
                        __instance.myCamera.targetTexture = newRt;
                        __instance.amplifyColorEffect.MaskTexture = newRt;
                    }
                    _lastWidth = width;
                }
            }
        }
    }
}