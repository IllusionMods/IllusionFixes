using BepInEx;
using Common;
using HarmonyLib;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class DynamicBonesFix : BaseUnityPlugin
    {
        public const string GUID = "Fix_DynamicBones";
        public const string PluginName = "Dynamic Bones Fix";

        internal void Main() => Harmony.CreateAndPatchAll(typeof(Hooks));

        internal static class Hooks
        {
            //Disable the SkipUpdateParticles method since it causes problems, namely causing jittering when the FPS is higher than 60
            [HarmonyPrefix, HarmonyPatch(typeof(DynamicBone), "SkipUpdateParticles")]
            internal static bool SkipUpdateParticles() => false;
            [HarmonyPrefix, HarmonyPatch(typeof(DynamicBone_Ver02), "SkipUpdateParticles")]
            internal static bool SkipUpdateParticlesVer02() => false;
        }
    }
}