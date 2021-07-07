using BepInEx;
using BepInEx.Logging;
using CharaCustom;
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
    }
}
