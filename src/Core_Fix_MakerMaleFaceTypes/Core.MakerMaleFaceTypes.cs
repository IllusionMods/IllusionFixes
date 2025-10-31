using BepInEx.Logging;

namespace IllusionFixes
{
    public partial class MakerMaleFaceTypes
    {
        public const string PluginName = "Maker Male Face Types";

        internal static new ManualLogSource Logger;

        private static MakerMaleFaceTypes Instance;

        public MakerMaleFaceTypes()
        {
            Instance = this;
            Logger = base.Logger;
        }

        private void Awake()
        {
            KKAPI.Maker.MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
        }
    }
}
