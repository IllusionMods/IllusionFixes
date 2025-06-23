using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChaCustom;
using Common;
using HarmonyLib;
using IllusionUtility.GetUtility;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Studio;
using System.Linq;
using Screencap;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IllusionFixes
{
    [BepInPlugin(PluginGUID, PluginName, Constants.PluginsVersion)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(ScreenshotManager.GUID, ScreenshotManager.Version)]
    public class EyebrowFix : BaseUnityPlugin
    {
        public const string PluginGUID = "KK_Fix_Eyebrow";
        public const string PluginName = "Eyebrow Outline Fix";
        internal static new ManualLogSource Logger;

        public static RenderTexture rt;
        public static Material mat;
        private static bool captureActive;
        public static ConfigEntry<bool> ConfigEnabled { get; private set; }

        private void Awake()
        {
            ConfigEnabled = Config.Bind("", "Enabled", false, "Whether the plugin is enabled. Restart the game after changing this setting.\n\nWarning: This plugin is still experiemental and not recommended for general use.");
            if (!ConfigEnabled.Value)
                return;

            Logger = base.Logger;

            mat = new Material(Shader.Find("Hidden/Internal-GUITextureClip"));

            Harmony.CreateAndPatchAll(typeof(Hooks));
            MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
            SceneManager.sceneLoaded += InitStudioUI;

            Camera.onPreCull += OnPreCull;

            ScreenshotManager.OnPreCapture += () => captureActive = true;
            ScreenshotManager.OnPostCapture += () => captureActive = false;
        }

        private void InitStudioUI(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name != "Studio") return;
            SceneManager.sceneLoaded -= InitStudioUI;

            var dropdownForegroundEyebrow = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/05_Etc/Eyebrows Draw/Dropdown").GetComponent<Dropdown>();
            dropdownForegroundEyebrow.onValueChanged.AddListener(value =>
            {
                var characters = StudioAPI.GetSelectedCharacters();
                foreach (var character in characters)
                    SetEyebrows(character.charInfo, (byte)value);
            });

            var dropdownForegroundEyes = GameObject.Find("StudioScene").transform.Find("Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/05_Etc/Eyes Draw/Dropdown").GetComponent<Dropdown>();
            dropdownForegroundEyes.onValueChanged.AddListener(value =>
            {
                var characters = StudioAPI.GetSelectedCharacters();
                foreach (var character in characters)
                    SetEyeliners(character.charInfo, (byte)value);
            });
        }

        //TODO: only do any of this allocation and deallocation of rendertextures when in a scene with an actual camera :^)
        //pre render
        private void Update()
        {
            int rx;
            int ry;
            if (captureActive)
            {
                rx = ScreenshotManager.ResolutionX.Value * ScreenshotManager.DownscalingRate.Value;
                ry = ScreenshotManager.ResolutionY.Value * ScreenshotManager.DownscalingRate.Value;
            }
            else
            {
                rx = Screen.width;
                ry = Screen.height;
            }

            if (rt == null || rt.width != rx || rt.height != ry)
            {
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                rt = RenderTexture.GetTemporary(rx, ry, 0, RenderTextureFormat.ARGBHalf);
            }
        }

        /// <summary>
        /// Clear RenderTexture before rendering
        /// </summary>
        internal static void OnPreCull(Camera cam)
        {
            if ((LayerName.CharaMask & cam.cullingMask) == 0)
                return;

            if (rt == null)
                return;

            var rta = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rta;
        }

        private void OnDestroy()
        {
            Camera.onPreCull -= OnPreCull;
        }

        /// <summary>
        /// Set up events to toggle on and off the eyebrow effect when the maker toggles are changed
        /// </summary>
        private void MakerAPI_MakerFinishedLoading(object sender, System.EventArgs e)
        {
            var customChangeMainMenu = FindObjectOfType<CustomChangeMainMenu>();
            CustomChangeFaceMenu ccFaceMenu = customChangeMainMenu.ccFaceMenu;

            //Eyebrows
            {
                Toggle[] tglForegroundEyebrow = ccFaceMenu.cvsEyebrow.tglForegroundEyebrow;

                tglForegroundEyebrow[0].onValueChanged.AddListener(value =>
                {
                    if (value)
                        SetEyebrows(MakerAPI.GetCharacterControl(), 0);
                });
                tglForegroundEyebrow[1].onValueChanged.AddListener(value =>
                {
                    if (value)
                        SetEyebrows(MakerAPI.GetCharacterControl(), 1);
                });
                tglForegroundEyebrow[2].onValueChanged.AddListener(value =>
                {
                    if (value)
                        SetEyebrows(MakerAPI.GetCharacterControl(), 2);
                });
            }

            //Eyeliners
            {
                Toggle[] tglForegroundEye = ccFaceMenu.cvsEye02.tglForegroundEye;

                tglForegroundEye[0].onValueChanged.AddListener(value =>
                {
                    if (value)
                        SetEyeliners(MakerAPI.GetCharacterControl(), 0);
                });
                tglForegroundEye[1].onValueChanged.AddListener(value =>
                {
                    if (value)
                        SetEyeliners(MakerAPI.GetCharacterControl(), 1);
                });
                tglForegroundEye[2].onValueChanged.AddListener(value =>
                {
                    if (value)
                        SetEyeliners(MakerAPI.GetCharacterControl(), 2);
                });
            }
        }

        /// <summary>
        /// Set the state of the eyebrows
        /// </summary>
        /// <param name="chaControl">Character this will be applied to</param>
        /// <param name="value">Value to set. 0 = from config, 1 = behind hair, 2 = in front of hair</param>
        public static void SetEyebrows(ChaControl chaControl, byte value)
        {
            if (MakerAPI.InsideMaker && !MakerAPI.InsideAndLoaded)
                return;

            if (value == 0) //From config
            {
                if (Manager.Config.EtcData.ForegroundEyebrow)
                    EnableEyebrows(chaControl);
                else
                    DisableEyebrows(chaControl);
            }
            else if (value == 1) //Behind hair
                DisableEyebrows(chaControl);
            else if (value == 2) //In front of hair
                EnableEyebrows(chaControl);
        }

        public static void EnableEyebrows(ChaControl chaControl)
        {
            chaControl.transform.FindLoop("cf_O_mayuge").GetOrAddComponent<Blitty>();
        }

        public static void DisableEyebrows(ChaControl chaControl)
        {
            var blitty = chaControl.transform.FindLoop("cf_O_mayuge").GetComponent<Blitty>();
            Destroy(blitty);
        }

        /// <summary>
        /// Set the state of the eyelines
        /// </summary>
        /// <param name="chaControl">Character this will be applied to</param>
        /// <param name="value">Value to set. 0 = from config, 1 = behind hair, 2 = in front of hair</param>
        public static void SetEyeliners(ChaControl chaControl, byte value)
        {
            if (MakerAPI.InsideMaker && !MakerAPI.InsideAndLoaded)
                return;

            if (value == 0) //From config
            {
                if (Manager.Config.EtcData.ForegroundEyes)
                    EnableEyeliners(chaControl);
                else
                    DisableEyeliners(chaControl);
            }
            else if (value == 1) //Behind hair
                DisableEyeliners(chaControl);
            else if (value == 2) //In front of hair
                EnableEyeliners(chaControl);
        }

        public static void EnableEyeliners(ChaControl chaControl)
        {
            chaControl.transform.FindLoop("cf_O_eyeline").GetOrAddComponent<Blitty>();
            chaControl.transform.FindLoop("cf_O_eyeline_low").GetOrAddComponent<Blitty>();
        }

        public static void DisableEyeliners(ChaControl chaControl)
        {
            var blitty = chaControl.transform.FindLoop("cf_O_eyeline").GetComponent<Blitty>();
            Destroy(blitty);
            blitty = chaControl.transform.FindLoop("cf_O_eyeline_low").GetComponent<Blitty>();
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

            EyebrowFix.SetEyebrows(__instance, __instance.chaFile.custom.face.foregroundEyebrow);
        }

        /// <summary>
        /// Happens after the character is loaded and eyeliner SMR initialized
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingEyelineUp))]
        private static void ChaControl_ChangeSettingEyelineUp(ChaControl __instance)
        {
            //Only care about maker and studio, other modes have problems that aren't worth the effort
            if (KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame)
                return;

            EyebrowFix.SetEyeliners(__instance, __instance.chaFile.custom.face.foregroundEyes);
        }
    }

    public class Blitty : MonoBehaviour
    {
        private SkinnedMeshRenderer smr;
        private Material[] materials;
        private CommandBuffer cb;
        private Camera mainCam;
        private readonly CameraEvent ev = CameraEvent.AfterForwardOpaque;
        private bool smrEnabledOriginal;

        private void Start()
        {
            smr = GetComponent<SkinnedMeshRenderer>();
            materials = smr.sharedMaterials.OrderBy(x => x.renderQueue).ToArray();
            mainCam = Camera.main;
        }

        private void OnDisable()
        {
            OnDestroy();
        }

        private void LateUpdate()
        {
            //Track the original state of the renderer (MaterialEditor compatibility)
            smrEnabledOriginal = smr.enabled;
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
            if (!smrEnabledOriginal)
                return;
            smr.enabled = false;
        }

        public void OnRenderObject()
        {
            OnDestroy();
            if (!smrEnabledOriginal)
                return;

            cb = new CommandBuffer();
            cb.SetRenderTarget(EyebrowFix.rt);
            foreach (var mat in materials)
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