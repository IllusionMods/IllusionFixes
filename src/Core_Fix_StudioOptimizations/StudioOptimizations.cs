using System.Collections;
using BepInEx;
using BepInEx.Harmony;
using HarmonyLib;
using Manager;
using Studio;

namespace IllusionFixes
{
    public partial class StudioOptimizations : BaseUnityPlugin
    {
        public const string GUID = "Fix_StudioOptimizations";
        public const string PluginName = "Studio Optimizations";

        private void Awake()
        {
            HarmonyWrapper.PatchAll(typeof(StudioOptimizations));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartScene), "LoadCoroutine")]
        private static void OverrideLoadCoroutine(ref IEnumerator __result)
        {
            __result = FastLoadCoroutine();
        }

        private static IEnumerator FastLoadCoroutine()
        {
            // Let the studio splash appear
            yield return null;

            // Run the whole load process immediately without wasting time rendering frames
            RunCoroutineImmediately(Singleton<Info>.Instance.LoadExcelDataCoroutine());

            Singleton<Scene>.Instance.LoadReserve(new Scene.Data
            {
                levelName = "Studio",
                // Turn off fading in to save more startup time
                isFade = false
            }, false);
        }

        private static void RunCoroutineImmediately(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is IEnumerator en)
                    RunCoroutineImmediately(en);
            }
        }
    }
}
