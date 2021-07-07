using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IllusionFixes
{
    public partial class MakerMaleFaceTypes
    {
        public const string PluginName = "Maker Male Face Types";

        internal static new ManualLogSource Logger;

        public MakerMaleFaceTypes()
        {
            Logger = base.Logger;
        }

        private void Awake()
        {
            KKAPI.Maker.MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
        }

        private void MakerAPI_MakerFinishedLoading(object sender, EventArgs e)
        {
            GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinFace/F_FaceType/Setting/Setting01/title").SetActive(true);
            GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinFace/F_FaceType/Setting/Setting01/SelectBox").SetActive(true);
            GameObject.Find("CharaCustom/CustomControl/CanvasSub/SettingWindow/WinFace/F_FaceType/Setting/Setting01/separate").SetActive(true);
        }
    }
}
