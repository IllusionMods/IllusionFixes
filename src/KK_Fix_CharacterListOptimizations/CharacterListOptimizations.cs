using ActionGame;
using BepInEx;
using ChaCustom;
using Common;
using ExtensibleSaveFormat;
using FreeH;
using HarmonyLib;
using Illusion.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine.UI;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)] // Need a completely separate version for KK Party
    [BepInProcess(Constants.VRProcessName)]
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
                h.Patch(vrType.GetMethod("InitializeList", AccessTools.all),
                    prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ClassRoomCharaFileInitializeListPrefix)),
                    postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ClassRoomCharaFileInitializeListPostfix)));
                h.Patch(vrType.GetMethod("Start", AccessTools.all),
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

                ReactiveProperty<ChaFileControl> info = Traverse.Create(__instance).Field("info").GetValue<ReactiveProperty<ChaFileControl>>();
                ClassRoomFileListCtrl listCtrl = Traverse.Create(__instance).Field("listCtrl").GetValue<ClassRoomFileListCtrl>();
                List<CustomFileInfo> lstFileInfo = Traverse.Create(listCtrl).Field("lstFileInfo").GetValue<List<CustomFileInfo>>();
                Button enterButton = Traverse.Create(__instance).Field("enterButton").GetValue<Button>();

                enterButton.onClick.RemoveAllListeners();
                enterButton.onClick.AddListener(() =>
                {
                    var onEnter = (Action<ChaFileControl>)AccessTools.Field(typeof(FreeHClassRoomCharaFile), "onEnter").GetValue(__instance);
                    string fullPath = lstFileInfo.First(x => x.FileName == info.Value.charaFileName.Remove(info.Value.charaFileName.Length - 4)).FullPath;

                    ChaFileControl chaFileControl = new ChaFileControl();
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
