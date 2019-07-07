using BepInEx;
using Common;
using Harmony;

namespace KK_Fix_ListFix
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class ListFix : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.miscfixes";
        public const string PluginName = "List Fix";

        private void Main()
        {
            if (!CommonCode.InsideKoikatsuParty)
                HarmonyInstance.Create(GUID).PatchAll(typeof(Hooks));
        }
    }
}
