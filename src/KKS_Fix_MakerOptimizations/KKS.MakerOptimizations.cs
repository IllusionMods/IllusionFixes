﻿using BepInEx;
using Common;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    //[BepInProcess(Constants.GameProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public partial class MakerOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KKS_Fix_MakerOptimizations";
    }
}