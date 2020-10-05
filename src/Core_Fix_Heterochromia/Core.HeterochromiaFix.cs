using BepInEx.Logging;
using HarmonyLib;

namespace IllusionFixes
{
    /// <summary>
    /// Sets the "Edit to Eye" radio buttons to "left" for characters with different pupils or gradients so that these things load properly in the character maker.
    /// </summary>
    public partial class HeterochromiaFix
    {
        public const string PluginName = "Character Maker Heterochromia Fix";
        internal static new ManualLogSource Logger;

        internal void Main()
        {
            Logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }
}
