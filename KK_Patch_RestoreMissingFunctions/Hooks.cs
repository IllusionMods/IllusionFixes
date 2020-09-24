using System;
using System.Collections;
using System.Collections.Generic;
using ChaCustom;
using Common;
using HarmonyLib;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using UniRx;

namespace IllusionFixes.Patchers
{
    public static partial class RestoreMissingFunctionsPatch
    {
        public static class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessoryAsync), typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool), typeof(bool))]
            private static void AccShakeHook(ChaControl __instance, ref IEnumerator __result, int slotNo)
            {
                __result = __result.AppendCo(() => ChangeShakeAccessory(__instance, slotNo));
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeHairAsync), typeof(int), typeof(int), typeof(bool), typeof(bool))]
            private static void HairShakeHook(ChaControl __instance, ref IEnumerator __result, int kind)
            {
                __result = __result.AppendCo(() => ChangeShakeHair(__instance, kind));
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CustomBase), "Awake")]
            private static void AwakeHook()
            {
                MakerAPI.MakerBaseLoaded -= MakerAPIOnMakerBaseLoaded;
                MakerAPI.MakerBaseLoaded += MakerAPIOnMakerBaseLoaded;
            }

            private static void MakerAPIOnMakerBaseLoaded(object sender, RegisterCustomControlsEvent e)
            {
                var l = new List<EventHandler>();
                void AddToggle(MakerCategory cat, int kind)
                {
                    var toggle = e.AddControl(new MakerToggle(cat, "Disable physics", null));
                    toggle.ValueChanged.Subscribe(x =>
                    {
                        var cc = MakerAPI.GetCharacterControl();
                        GetNoShakeProp(cc.fileHair.parts[0]).Value = x;
                        ChangeShakeHair(cc, kind);
                    });
                    void UpdateHairToggle(object o, EventArgs args) => toggle.Value = GetNoShakeProp(MakerAPI.GetCharacterControl().fileHair.parts[kind]).Value;
                    l.Add(UpdateHairToggle);
                    MakerAPI.ReloadCustomInterface += UpdateHairToggle;
                }

                AddToggle(MakerConstants.Hair.Back, (int)ChaFileDefine.HairKind.back);
                AddToggle(MakerConstants.Hair.Front, (int)ChaFileDefine.HairKind.front);
                AddToggle(MakerConstants.Hair.Side, (int)ChaFileDefine.HairKind.side);
                AddToggle(MakerConstants.Hair.Extension, (int)ChaFileDefine.HairKind.option);

                var accToggle = new AccessoryControlWrapper<MakerToggle, bool>(MakerAPI.AddAccessoryWindowControl(new MakerToggle(null, "Disable physics", null)));
                accToggle.ValueChanged += (o, args) =>
                {
                    var cc = MakerAPI.GetCharacterControl();
                    //var acc = AccessoriesApi.GetPartsInfo(args.SlotIndex);
                    //GetNoShakeProp(acc).Value = args.NewValue;
                    GetNoShakeProp(cc.nowCoordinate.accessory.parts[args.SlotIndex]).Value = args.NewValue;
                    GetNoShakeProp(cc.chaFile.coordinate[cc.chaFile.status.coordinateType].accessory.parts[args.SlotIndex]).Value = args.NewValue;
                    ChangeShakeAccessory(cc, args.SlotIndex);
                };
                accToggle.VisibleIndexChanged += (o, args) => accToggle.Control.Visible.OnNext(args.SlotIndex < 20);

                void ReloadAccToggle(object o, EventArgs args)
                {
                    var cc = MakerAPI.GetCharacterControl();
                    //var accCount = AccessoriesApi.GetCvsAccessoryCount();
                    for (var i = 0; i < cc.nowCoordinate.accessory.parts.Length; i++)
                    {
                        accToggle.SetValue(i, GetNoShakeProp(cc.nowCoordinate.accessory.parts[i]).Value);
                    }
                }
                l.Add(ReloadAccToggle);
                MakerAPI.ReloadCustomInterface += ReloadAccToggle;

                MakerAPI.MakerExiting += (o, args) =>
                {
                    foreach (var handler in l) MakerAPI.ReloadCustomInterface -= handler;
                    l.Clear();
                };
            }

            private static Traverse<bool> GetNoShakeProp(object obj)
            {
                return Traverse.Create(obj).Property<bool>("noShake");
            }

            public static void ChangeShakeHair(ChaControl __instance, int parts)
            {
                if (__instance.objHair[parts])
                {
                    foreach (var dynamicBone in __instance.objHair[parts].GetComponentsInChildren<DynamicBone>(true))
                        dynamicBone.enabled = !GetNoShakeProp(__instance.fileHair.parts[parts]).Value;
                }
            }

            public static void ChangeShakeAccessory(ChaControl __instance, int slotNo)
            {
                if (__instance.objAccessory.Length > slotNo && __instance.objAccessory[slotNo])
                {
                    var noshake = !GetNoShakeProp(__instance.nowCoordinate.accessory.parts[slotNo]).Value;
                    foreach (var dynamicBone in __instance.objAccessory[slotNo].GetComponentsInChildren<DynamicBone>(true))
                    {
                        dynamicBone.enabled = noshake;
                    }
                }
            }
        }
    }
}