using ActionGame.Chara;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using Common;
using HarmonyLib;
using Manager;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    [BepInProcess(Constants.GameProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    [DefaultExecutionOrder(-1000)]
    public class MainGameOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_MainGameOptimizations";
        public const string PluginName = "Main Game Optimizations";

        public static ConfigEntry<bool> AsyncClothesLoading { get; private set; }
        public static ConfigEntry<bool> PreloadCharacters { get; private set; }
        public static ConfigEntry<bool> ThrottleCharaUpdates { get; private set; }

        internal void Awake()
        {
            if (IncompatiblePluginDetector.AnyIncompatiblePlugins()) return;

            if (CommonCode.InsideStudio) return;

            AsyncClothesLoading = Config.Bind(Utilities.ConfigSectionTweaks, "Async clothes loading", true, new ConfigDescription("Spread loading of clothes in school roam mode over multiple frames. Greatly reduces seemingly random stutters when characters change clothes somewhere in the world.\nWarning: In rare cases can cause some visual glitches like 2 coordinates loaded at once."));
            PreloadCharacters = Config.Bind(Utilities.ConfigSectionTweaks, "Preload characters on initial load", true, new ConfigDescription("Forces all characters to load during initial load into school mode. Slightly longer loading time but eliminates large stutters when unseen characters enter current map."));
            ThrottleCharaUpdates = Config.Bind(Utilities.ConfigSectionTweaks, "Throttle chara blend shape updates", true, new ConfigDescription("Reduces the amount of unnecessary blend shape updates. Performance improvement in main game, especially with over 20 characters in one room."));

            HarmonyWrapper.PatchAll(typeof(MainGameOptimizations));

            SceneManager.sceneLoaded += (arg0, mode) => _runningReloadCoroutines.Clear();
        }


        #region Async clothes load

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

        #endregion

        #region Preload characters

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
                var origPos = instance.position;
                if (Game.Instance != null && Game.Instance.Player != null && Game.Instance.Player.transform != null)
                    instance.position = Game.Instance.Player.transform.position;

                // SetActive(true) that will always load the character, even if it's not on current map. Need to hide it after.
                instance.SetActive(true);
                if (!isPop)
                    instance.SetActive(false);

                instance.position = origPos;
            }
            else
            {
                instance.SetActive(isPop);
            }
        }

        #endregion

        #region Reduce blend shape update spam

        private static readonly Queue<KeyValuePair<int, ChaControl>> _chaUpdateStatusQueue = new Queue<KeyValuePair<int, ChaControl>>();
        private static readonly SortedDictionary<int, ChaControl> _throttledDict = new SortedDictionary<int, ChaControl>();
        private static Vector3? _playerPos;
        private static bool _needsFullCharaUpdate;

        private void Update()
        {
            // Turn off if can't determine the current game state or if turning it on will not bring any improvement
            if (!ThrottleCharaUpdates.Value ||
                // Only run in main game
                !Manager.Scene.IsInstance() || !Game.IsInstance() || Game.Instance.actScene == null ||
                // less than 5 characters will degrade perf
                Game.Instance.HeroineList.Count < 6 ||
                // Fixes breaking mouths in invite events
                Game.Instance.actScene.isEventNow || Manager.Scene.Instance.IsNowLoadingFade || ADV.Program.isADVProcessing)
            {
                _needsFullCharaUpdate = true;
                return;
            }

            // Always run all updates in talk / h scenes since the number of characters is limited
            // Action == roaming mode, talk scene is loaded into AddSceneName
            var scene = Manager.Scene.Instance;
            if (scene.LoadSceneName != "Action" || !string.IsNullOrEmpty(scene.AddSceneName))
            {
                _needsFullCharaUpdate = true;
                return;
            }

            _needsFullCharaUpdate = false;
            _playerPos = null;
            _throttledDict.Clear();

            // Cache player position for the face updates since they get triggered many many more times every frame
            if (Game.Instance.Player != null && Game.Instance.Player.transform != null)
                _playerPos = Game.Instance.Player.transform.position;

            // Throttle down character updates to max 4 per frame (and turn them off during loading, above)
            if (Character.IsInstance())
            {
                var allCharaEntries = Character.Instance.dictEntryChara;
                for (int i = 0; i < 4 && _chaUpdateStatusQueue.Count > 0; i++)
                {
                    var value = _chaUpdateStatusQueue.Dequeue();
                    // Make sure the character didn't get removed before we got to it
                    if (allCharaEntries.ContainsKey(value.Key))
                        _throttledDict[value.Key] = value.Value;
                }

                if (_chaUpdateStatusQueue.Count == 0)
                {
                    foreach (var value in allCharaEntries)
                        _chaUpdateStatusQueue.Enqueue(value);
                }
            }
        }

        private static SortedDictionary<int, ChaControl> GetThrottledChaDict(Character instance)
        {
            return _needsFullCharaUpdate ? instance.dictEntryChara : _throttledDict;
        }

        private static IEnumerable<CodeInstruction> PatchCharaDic(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call &&
                    instruction.operand is MethodInfo mi &&
                    mi.Name == "get_dictEntryChara")
                {
                    instruction.operand = AccessTools.Method(typeof(MainGameOptimizations),
                        nameof(MainGameOptimizations.GetThrottledChaDict));
                }

                yield return instruction;
            }
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(Character), "Update")]
        public static IEnumerable<CodeInstruction> CharacterUpdate(IEnumerable<CodeInstruction> instructions)
        {
            return PatchCharaDic(instructions);
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(Character), "LateUpdate")]
        public static IEnumerable<CodeInstruction> CharacterLateUpdate(IEnumerable<CodeInstruction> instructions)
        {
            return PatchCharaDic(instructions);
        }

        /// <summary>
        /// Only update faces of characters near the player
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(FaceBlendShape), "LateUpdate")]
        public static bool FaceBlendShapeLateUpdate(FaceBlendShape __instance)
        {
            if (_needsFullCharaUpdate)
                return true;
            // Don't update during loading screens
            if (_throttledDict.Count == 0)
                return false;
            // If player is not found fall back to always updating
            if (!_playerPos.HasValue)
                return true;
            // Update when player is close (4 units away)
            if ((__instance.transform.position - _playerPos.Value).sqrMagnitude < 4 * 4)
                return true;
            return false;
        }

        #endregion
    }
}
