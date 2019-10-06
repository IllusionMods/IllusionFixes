using ActionGame.Chara;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using Common;
using HarmonyLib;
using Manager;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class MainGameOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_MainGameOptimizations";
        public const string PluginName = "Main Game Optimizations";

        public static ConfigWrapper<bool> AsyncClothesLoading { get; private set; }
        public static ConfigWrapper<bool> PreloadCharacters { get; private set; }

        internal void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            if (CommonCode.InsideStudio) return;

            AsyncClothesLoading = Utilities.FixesConfig.Wrap(Utilities.ConfigSectionTweaks, "Async clothes loading", "Spread loading of clothes in school roam mode over multiple frames. Greatly reduces seemingly random stutters when characters change clothes somewhere in the world.\nWarning: In rare cases can cause some visual glitches like 2 coordinates loaded at once.", true);
            PreloadCharacters = Utilities.FixesConfig.Wrap(Utilities.ConfigSectionTweaks, "Preload characters on initial load", "Forces all characters to load during initial load into school mode. Slightly longer loading time but eliminates large stutters when unseen characters enter current map.", true);

            HarmonyWrapper.PatchAll(typeof(MainGameOptimizations));

            SceneManager.sceneLoaded += (arg0, mode) => _runningReloadCoroutines.Clear();
        }

        private static readonly List<ChaControl> _runningReloadCoroutines = new List<ChaControl>();

        /// <summary>
        /// If characters change clothes anywhere, new clothes get loaded synchronously by default, causing 
        /// seemingly random stutter. Use async loading instead to mitigate this
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(NPC), nameof(NPC.SynchroCoordinate))]
        public static bool SynchroCoordinateOverride(NPC __instance, bool isRemove)
        {
            if (!AsyncClothesLoading.Value)
                return true;

            // If not visible, do async loading of the clothes instead, replaces the original method
            if (__instance.chaCtrl == null || !Character.IsInstance())
                return false;

            var nowCoordinate = __instance.heroine.NowCoordinate;
            if (__instance.chaCtrl.fileStatus.coordinateType == nowCoordinate) return false;

            if (!__instance.chaCtrl.ChangeCoordinateType((ChaFileDefine.CoordinateType)nowCoordinate)) return false;

            Singleton<Character>.Instance.enableCharaLoadGCClear = false;
            _runningReloadCoroutines.Add(__instance.chaCtrl);

            // Do this before starting to reload clothes in case player is nearby to make it look less weird
            if (isRemove)
                __instance.chaCtrl.RandomChangeOfClothesLowPoly(__instance.heroine.lewdness);

            var isActive = __instance.chaCtrl.GetActiveTop();

            // Prevent the character from doing anything while clothes load
            __instance.Pause(true);

            var reloadCoroutine =
                // Async version of the reload that's implemented but is never actually used ¯\_(ツ)_/¯
                __instance.chaCtrl.ReloadAsync(false, true, true, true, true)
                // Let the game settle down, running next reload so fast can possibly make 2 sets of clothes spawn?
                .AppendCo(new WaitForEndOfFrame())
                .AppendCo(new WaitForEndOfFrame())
                .AppendCo(
                    () =>
                    {
                        // Second normal reload needed to fix clothes randomly not loading fully, goes 
                        // very fast since assets are loaded by the async version by now
                        __instance.chaCtrl.Reload(false, true, true, true);

                        _runningReloadCoroutines.Remove(__instance.chaCtrl);
                        _runningReloadCoroutines.RemoveAll(c => c == null);
                        if (_runningReloadCoroutines.Count == 0)
                            Singleton<Character>.Instance.enableCharaLoadGCClear = true;

                        __instance.Pause(false);
                    });

            __instance.chaCtrl.StartCoroutine(reloadCoroutine);

            // Needed to counter SetActiveTop(false) at the start of ReloadAsync. That code is before 1st yield so 
            // it is executed in the current "thread" before returning to this call after 1st yield is encountered
            __instance.chaCtrl.SetActiveTop(isActive);

            return false;
        }

        /// <summary>
        /// Force all characters to load during the initial game loading period to reduce stutter.
        /// By default they are not loaded until they appear on currently visible map, lagging the game
        /// </summary>
        [HarmonyTranspiler, HarmonyPatch(typeof(NPC), nameof(NPC.ReStart))]
        public static IEnumerable<CodeInstruction> NPCReStartTpl(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(Base), nameof(Base.SetActive));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var replacement = AccessTools.Method(typeof(MainGameOptimizations), nameof(ForcedSetActive));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));

            foreach (var codeInstruction in instructions)
            {
                if (codeInstruction.operand == target)
                {
                    codeInstruction.opcode = OpCodes.Call;
                    codeInstruction.operand = replacement;
                }
                yield return codeInstruction;
            }
        }

        public static void ForcedSetActive(Base instance, bool isPop)
        {
            if (PreloadCharacters.Value)
            {
                // SetActive(true) that will always load the character, even if it's not on current map. Need to hide it after.
                instance.SetActive(true);
                if (!isPop)
                    instance.SetActive(false);
            }
            else
            {
                instance.SetActive(isPop);
            }
        }
    }
}
