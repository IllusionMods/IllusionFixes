using System.Linq;
using System.Reflection;
using BepInEx;
using Common;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using Manager;
using UniRx;

namespace IllusionFixes
{
    /// <summary>
    /// Adds missing head type toggle in KK Party maker. Not needed in KK.
    /// </summary>
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInDependency(KoikatuAPI.GUID, "1.7")]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class RestoreMissingFunctions : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_RestoreMissingFunctions";
        public const string PluginName = "Restore missing functions";

        private void Awake()
        {
            var missingDarkness = typeof(ChaInfo).GetProperty("exType", BindingFlags.Public | BindingFlags.Instance) == null;

            if (missingDarkness)
            {
                Logger.LogInfo("Darkness/Yoyaku expansion is missing!");
                MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
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
    }
}