using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using Common;
using Config;
using HarmonyLib;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using Manager;
using UniRx;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInDependency(KoikatuAPI.GUID, "1.7")]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class RestoreMissingFunctions : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_RestoreMissingFunctions";
        public const string PluginName = "Restore missing functions";

        private void Awake()
        {
            // Add missing head type selection if the game supports it (ui for it was added in darkness for some reason)
            var missingDarkness = typeof(ChaInfo).GetProperty("exType", BindingFlags.Public | BindingFlags.Instance) == null;
            if (missingDarkness)
            {
                Logger.LogInfo("Darkness/Yoyaku expansion is missing!");

                // Make sure this setting is supported // todo is this necessary?
                if (Enum.IsDefined(typeof(ChaListDefine.CategoryNo), 100))
                    MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
            }

            // Fixes only needed for party
            if (Paths.ProcessName == Constants.GameProcessNameSteam)
            {
                var h = new Harmony(GUID);
                h.Patch(AccessTools.Method(typeof(ConfigScene), "Start"),
                    postfix: new HarmonyMethod(typeof(RestoreMissingFunctions), nameof(ConfigAddFix)));
                h.Patch(AccessTools.Method("Localize.Translate.Manager:SetLanguage", new[] { typeof(int) }),
                    transpiler: new HarmonyMethod(typeof(RestoreMissingFunctions), nameof(LanguageUnlock)));
            }
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            Logger.LogInfo("Adding a head type dropdown to maker");

            var chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
            var categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.bo_head);
            var headValues = categoryInfo.Values.ToList();

            var dd = e.AddControl(new MakerDropdown("Head type", headValues.Select(x => x.Name).ToArray(), MakerConstants.Face.All, 0, this));
            MakerAPI.ReloadCustomInterface += (o, args) => dd.Value = headValues.FindIndex(i => i.Id == MakerAPI.GetCharacterControl().infoHead.Id);
            dd.ValueChanged.Subscribe(x => MakerAPI.GetCharacterControl().ChangeHead(headValues[x].Id));
        }

        private static void ConfigAddFix(ConfigScene __instance, ref IEnumerator __result)
        {
            BaseSetting setting = null;

            __result = __result
                .AppendCo(new WaitForEndOfFrame())
                .AppendCo(() =>
                {
                    try
                    {
                        var add = __instance.transform.Find("Canvas/imgWindow/ScrollView/Content/Node Addtional");

                        if (add && add.gameObject.activeInHierarchy)
                        {
                            var add20Prop = Traverse.Create<Game>().Property("isAdd20");
                            if (add20Prop.FieldExists() && add20Prop.GetValue<bool>())
                            {
                                var add20Tr = __instance.transform.Find("Canvas/imgWindow/ScrollView/Content/Node Addtional_20");
                                if (add20Tr)
                                {
                                    add20Tr.gameObject.SetActive(true);
                                    add20Tr.SetSiblingIndex(add.GetSiblingIndex());
                                    setting = add20Tr.GetComponent<BaseSetting>();
                                    add.gameObject.SetActive(false);
                                }
                            }
                            else
                            {
                                var addNormalTr = __instance.transform.Find("Canvas/imgWindow/ScrollView/Content/Node Addtional_normal");
                                if (addNormalTr)
                                {
                                    addNormalTr.gameObject.SetActive(true);
                                    addNormalTr.SetSiblingIndex(add.GetSiblingIndex());
                                    setting = addNormalTr.GetComponent<BaseSetting>();
                                    add.gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                })
                .AppendCo(new WaitForEndOfFrame())
                .AppendCo(() =>
                {
                    if (setting != null)
                    {
                        // Need to wait until next frame to init because mobcolor setting will be corrupted if inited before colorpicker Start runs
                        setting.Init();
                        setting.UIPresenter();
                    }
                });
        }

        private static IEnumerable<CodeInstruction> LanguageUnlock(IEnumerable<CodeInstruction> instructions)
        {
            var ins = instructions.ToList();
            // Remove language check at the start of the method
            var i = ins.FindIndex(instruction => instruction.opcode == OpCodes.Starg_S);
            if (i > 0) ins.RemoveRange(0, i + 1);
            return ins;
        }
    }
}