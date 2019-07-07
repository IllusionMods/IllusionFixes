using BepInEx;
using Common;
using Harmony;

namespace KK_Fix_PersonalityCorrector
{
    /// <summary>
    /// Changes any invalid personalities to the "Pure" personality to prevent the game from breaking when adding them to the class
    /// </summary>
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class PersonalityCorrector : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.personalitycorrector";
        public const string PluginName = "Personality Corrector";

        private void Main()
        {
            if (!CommonCode.InsideKoikatsuParty)
                HarmonyInstance.Create(GUID).PatchAll(typeof(Hooks));
        }
    }
}
