using BepInEx;
using CharaCustom;
using Common;
using System;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class MakerMaleFaceTypes : BaseUnityPlugin
    {
        public const string GUID = "AI_Fix_MakerMaleFaceTypes";

        internal void MakerFinishedLoading()
        {
            CvsF_FaceType faceType = GameObject.Find("CharaCustom").transform.GetComponentInChildren<CvsF_FaceType>();
            faceType.transform.Find("Setting/Setting01/title").gameObject.SetActive(true);
            faceType.transform.Find("Setting/Setting01/SelectBox").gameObject.SetActive(true);
            faceType.transform.Find("Setting/Setting01/separate").gameObject.SetActive(true);
        }
    }
}
