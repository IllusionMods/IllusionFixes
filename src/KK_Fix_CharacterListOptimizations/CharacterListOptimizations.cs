using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using BepInEx;
using ChaCustom;
using Common;
using ExtensibleSaveFormat;
using FreeH;
using HarmonyLib;
using Illusion.Game;
using UniRx;
using UnityEngine.UI;
#pragma warning disable KKANAL03
#pragma warning disable KKANAL04

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)] // Need a completely separate version for KK Party
    [BepInProcess(Constants.VRProcessName)]
    [BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class CharacterListOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_CharacterListOptimizations";
        public const string PluginName = "Character List Optimizations";

        internal void Awake()
        {
            var h = Harmony.CreateAndPatchAll(typeof(Hooks));

            // The VR classes are only available in VR module so they can't be directly referenced
            var vrType = Type.GetType("VR.VRClassRoomCharaFile, Assembly-CSharp");
            if (vrType != null)
            {
                var startMethod = vrType.GetMethod("Start", AccessTools.all);
                Logger.LogDebug("Found VR character list, patching: " + startMethod.FullDescription());
                // In VR the list initialization code is inlined into Start, unlike main game
                h.Patch(startMethod,
                    prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ClassRoomCharaFileInitializeListPrefix)),
                    postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ClassRoomCharaFileInitializeListPostfix)));
                h.Patch(startMethod,
                    postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ClassRoomCharaFileStartPostfix)));
            }
        }

        internal class Hooks
        {
            #region Free H List
            /// <summary>
            /// Turn off ExtensibleSaveFormat events
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
            internal static void FreeHClassRoomCharaFileStartPrefix() => ExtendedSave.LoadEventsEnabled = false;
            /// <summary>
            /// Turn back on ExtensibleSaveFormat events, load a copy of the character with extended data on this time, and use that instead.
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
            internal static void FreeHClassRoomCharaFileStartPostfix(FreeHClassRoomCharaFile __instance)
            {
                ExtendedSave.LoadEventsEnabled = true;

                var info = __instance.info;
                var lstFileInfo = __instance.listCtrl.lstFileInfo;
                var enterButton = __instance.enterButton;

                enterButton.onClick.RemoveAllListeners();
                enterButton.onClick.AddListener(() =>
                {
                    var onEnter = (Action<ChaFileControl>)AccessTools.Field(typeof(FreeHClassRoomCharaFile), "onEnter").GetValue(__instance);
                    var fullPath = lstFileInfo.First(x => x.FileName == info.Value.charaFileName.Remove(info.Value.charaFileName.Length - 4)).FullPath;

                    var chaFileControl = new ChaFileControl();
                    chaFileControl.LoadCharaFile(fullPath, info.Value.parameter.sex, false, true);

                    onEnter(chaFileControl);
                    Utils.Sound.Play(SystemSE.sel);
                });
            }
            #endregion

            #region Classroom list / VR Free H list
            /// <summary>
            /// Turn off ExtensibleSaveFormat events
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(ClassRoomCharaFile), "InitializeList")]
            internal static void ClassRoomCharaFileInitializeListPrefix() => ExtendedSave.LoadEventsEnabled = false;
            /// <summary>
            /// Turn back on ExtensibleSaveFormat events
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(ClassRoomCharaFile), "InitializeList")]
            internal static void ClassRoomCharaFileInitializeListPostfix() => ExtendedSave.LoadEventsEnabled = true;
            /// <summary>
            /// Load a copy of the character with extended data on this time, and use that instead.
            /// Need to keep the object types vague since this is also used for VR list which is of different type
            /// </summary>
            [HarmonyPostfix, HarmonyPatch(typeof(ClassRoomCharaFile), "Start")]
            internal static void ClassRoomCharaFileStartPostfix(object __instance)
            {
                var traverse = Traverse.Create(__instance);
                var info = traverse.Field("info").GetValue<ReactiveProperty<ChaFileControl>>();
                var lstFileInfo = traverse.Field("listCtrl").Field("lstFileInfo").GetValue<List<CustomFileInfo>>();
                var enterButton = traverse.Field("enterButton").GetValue<Button>();

                enterButton.onClick.RemoveAllListeners();
                enterButton.onClick.AddListener(() =>
                {
                    var onEnter = traverse.Field("onEnter").GetValue<Action<ChaFileControl>>();
                    string fullPath = lstFileInfo.First(x => x.FileName == info.Value.charaFileName.Remove(info.Value.charaFileName.Length - 4)).FullPath;

                    ChaFileControl chaFileControl = new ChaFileControl();
                    chaFileControl.LoadCharaFile(fullPath, info.Value.parameter.sex, false, true);

                    onEnter(chaFileControl);
                    Utils.Sound.Play(SystemSE.sel);
                });
            }
            #endregion
        }
    }
}
