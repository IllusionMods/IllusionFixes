using System.Linq;
using BepInEx;
using Common;
using IllusionFixes;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using Manager;
using UniRx;

namespace KK_Fix_SteamMissingFunctions
{
    /// <summary>
    /// Adds missing head type toggle in KK Party maker. Not needed in KK.
    /// </summary>
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInDependency(KoikatuAPI.GUID, "1.7")]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class SteamMissingFunctions : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_SteamMissingFunctions";
        public const string PluginName = "Koikatsu Party missing function restore";

        private void Awake()
        {
            MakerAPI.RegisterCustomSubCategories += MakerAPI_RegisterCustomSubCategories;
        }

        private void MakerAPI_RegisterCustomSubCategories(object sender, RegisterSubCategoriesEvent e)
        {
            var chaListCtrl = Singleton<Character>.Instance.chaListCtrl;
            var categoryInfo = chaListCtrl.GetCategoryInfo(ChaListDefine.CategoryNo.bo_head);
            var headValues = categoryInfo.Values.ToList();

            var dd = e.AddControl(new MakerDropdown("Head type", headValues.Select(x => x.Name).ToArray(), MakerConstants.Face.All, 0, this));
            MakerAPI.ReloadCustomInterface += (o, args) => dd.Value = headValues.FindIndex(i => i.Id == MakerAPI.GetCharacterControl().infoHead.Id);
            dd.ValueChanged.Subscribe(x => MakerAPI.GetCharacterControl().ChangeHead(headValues[x].Id));
        }
    }
}