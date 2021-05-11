using BepInEx;
using BepInEx.Logging;
using ChaCustom;
using HarmonyLib;
using IllusionUtility.GetUtility;
using KKAPI;
using KKAPI.Maker;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace IllusionFixes
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class EyebrowFix : BaseUnityPlugin
    {
        public const string PluginGUID = "KK_Fix_Eyebrow";
        public const string PluginName = "Eyebrow Outline Fix";
        public const string PluginVersion = "1.0";
        internal static new ManualLogSource Logger;

        /*
          TODO:
            - EC version
            - Adapt code for eyeliners as well
            - Rename plugin since EyebrowFix doesn't make sense if it also works on eyeliners
            - Fix compatibility with MaterialEditor disabled renderers
        */

        public static RenderTexture rt;
        public static Material mat;

        internal void Awake()
        {
            Logger = base.Logger;

            mat = new Material(Shader.Find("Hidden/Internal-GUITextureClip"));

            Harmony.CreateAndPatchAll(typeof(Hooks));
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
        }

        //TODO: only do any of this allocation and deallocation of rendertextures when in a scene with an actual camera :^)
        //pre render
        private void Update()
        {
            if (rt != null)
            {
                RenderTexture.ReleaseTemporary(rt);
                rt = null;
            }

            int rx;
            int ry;

            if (Screencap.ScreenshotManager.KeyCaptureAlpha.Value.IsDown())
            {
                rx = Screencap. ScreenshotManager.ResolutionX.Value * Screencap.ScreenshotManager.DownscalingRate.Value;
                ry = Screencap.ScreenshotManager.ResolutionY.Value * Screencap.ScreenshotManager.DownscalingRate.Value;
            }
            else
            {
                rx = Screen.width;
                ry = Screen.height;
            }
            rt = RenderTexture.GetTemporary(rx, ry, 0, RenderTextureFormat.ARGBHalf);
            var rta = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rta;
        }

        /// <summary>
        /// Set up events to toggle on and off the eyebrow effect when the maker toggles are changed
        /// </summary>
        private void MakerAPI_MakerFinishedLoading(object sender, System.EventArgs e)
        {
            var customChangeMainMenu = FindObjectOfType<CustomChangeMainMenu>();
            CustomChangeFaceMenu ccFaceMenu = (CustomChangeFaceMenu)Traverse.Create(customChangeMainMenu).Field("ccFaceMenu").GetValue();
            CvsEyebrow cvsEyebrow = (CvsEyebrow)Traverse.Create(ccFaceMenu).Field("cvsEyebrow").GetValue();
            Toggle[] tglForegroundEyebrow = (Toggle[])Traverse.Create(cvsEyebrow).Field("tglForegroundEyebrow").GetValue();

            //From config
            tglForegroundEyebrow[0].onValueChanged.AddListener(value =>
            {
                if (value)
                    if (Manager.Config.EtcData.ForegroundEyebrow)
                        Enable(MakerAPI.GetCharacterControl());
                    else
                        Disable(MakerAPI.GetCharacterControl());
            });
            //Not showing through hair
            tglForegroundEyebrow[1].onValueChanged.AddListener(value =>
            {
                if (value)
                    Disable(MakerAPI.GetCharacterControl());
            });
            //Showing through hair
            tglForegroundEyebrow[2].onValueChanged.AddListener(value =>
            {
                if (value)
                    Enable(MakerAPI.GetCharacterControl());
            });
        }

        public static void Enable(ChaControl chaControl)
        {
            chaControl.transform.FindLoop("cf_O_mayuge").GetOrAddComponent<Blitty>();
        }

        public static void Disable(ChaControl chaControl)
        {
            var blitty = chaControl.transform.FindLoop("cf_O_mayuge").GetComponent<Blitty>();
            Destroy(blitty);
        }
    }

    internal static class Hooks
    {
        [HarmonyPrefix, HarmonyPatch(typeof(AmplifyColorBase), "OnRenderImage")]
        private static void AmplifyColorBase_OnRenderImage(ref RenderTexture source)
        {
            if (EyebrowFix.rt == null)
                return;
            Graphics.Blit(EyebrowFix.rt, source, EyebrowFix.mat); //blit into whatever the camera sees before applying first post effect (which is apparently ACE)
        }

        /// <summary>
        /// Happens after the character is loaded and eyebrow SMR initialized
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingEyebrow))]
        private static void ChaControl_ChangeSettingEyebrow(ChaControl __instance)
        {
            //Only care about maker and studio, other modes have problems that aren't worth the effort
            if (KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame)
                return;

            if (__instance.chaFile.custom.face.foregroundEyebrow == 2) //In front of hair
                EyebrowFix.Enable(__instance);
            else if (__instance.chaFile.custom.face.foregroundEyebrow == 0 && Manager.Config.EtcData.ForegroundEyebrow)
                EyebrowFix.Enable(__instance);
        }
    }

    public class Blitty : MonoBehaviour
    {
        private SkinnedMeshRenderer smr;
        private Material mat; //TODO: do we need this
        private CommandBuffer cb;
        private Camera mainCam;
        private readonly CameraEvent ev = CameraEvent.AfterForwardOpaque;

        private void Start()
        {
            smr = GetComponent<SkinnedMeshRenderer>();
            mat = smr.sharedMaterial;
            mainCam = Camera.main;
        }

        private void OnWillRenderObject()
        {
            /*
                Timing sensitive. We want to achieve the following goals:
                - Hide the vanilla eyebrows
                - Show them before commandbuffer executes
                - Ensure the skinnedmeshrenderer animates
                This method apparently fulfils those criteria
            */
            smr.enabled = false;
        }

        public void OnRenderObject()
        {
            OnDestroy();

            cb = new CommandBuffer();
            cb.SetRenderTarget(EyebrowFix.rt);
            cb.DrawRenderer(smr, mat, 0, 0);
            smr.enabled = true;
            mainCam.AddCommandBuffer(ev, cb);
        }

        private void OnDestroy()
        {
            if (cb != null && mainCam)
                mainCam.RemoveCommandBuffer(ev, cb);
        }
    }
}