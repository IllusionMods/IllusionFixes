using System;
using BepInEx;
using CharaCustom;
using Common;
using KKAPI.Maker;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class MakerMaleFaceTypes : BaseUnityPlugin
    {
        public const string GUID = "HS2_Fix_MakerMaleFaceTypes";

        private void MakerAPI_MakerFinishedLoading(object sender, EventArgs e)
        {
            if (MakerAPI.GetMakerSex() == 0)
            {
                CvsF_FaceType faceType = GameObject.Find("CharaCustom").transform.GetComponentInChildren<CvsF_FaceType>();
                faceType.transform.Find("Setting/Setting01/title").gameObject.SetActive(true);
                faceType.transform.Find("Setting/Setting01/SelectBox").gameObject.SetActive(true);
                faceType.transform.Find("Setting/Setting01/separate").gameObject.SetActive(true);
            }
        }
    }
}
