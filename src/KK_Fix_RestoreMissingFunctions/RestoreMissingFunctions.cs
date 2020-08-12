using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx;
using Common;
using HarmonyLib;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using Manager;
using UniRx;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInDependency(KoikatuAPI.GUID, "1.7")]
    [BepInIncompatibility("KK_FixPartySettings")]
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

            // ConfigAddFix is only needed in party
            if (Paths.ProcessName == Constants.GameProcessNameSteam)
                Harmony.CreateAndPatchAll(typeof(RestoreMissingFunctions));
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ConfigScene), "Start")]
        private static void ConfigAddFix(ConfigScene __instance, ref IEnumerator __result)
        {
            __result = __result.AppendCo(() =>
            {
                var add = __instance.transform.Find("Canvas/imgWindow/ScrollView/Content/Node Addtional");

                if (add && add.gameObject.activeInHierarchy)
                {
                    if (Game.isAdd20)
                    {
                        var add20 = __instance.transform.Find("Canvas/imgWindow/ScrollView/Content/Node Addtional_20");
                        if (add20)
                        {
                            add20.gameObject.SetActive(true);
                            add20.SetSiblingIndex(add.GetSiblingIndex());
                            add.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        var addNormal = __instance.transform.Find("Canvas/imgWindow/ScrollView/Content/Node Addtional_normal");
                        if (addNormal)
                        {
                            addNormal.gameObject.SetActive(true);
                            addNormal.SetSiblingIndex(add.GetSiblingIndex());
                            add.gameObject.SetActive(false);
                        }
                    }
                }
            });
        }
    }
}