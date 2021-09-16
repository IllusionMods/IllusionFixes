using ActionGame.Chara;
using BepInEx;
using BepInEx.Configuration;
using Common;
using HarmonyLib;
using Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = Manager.Scene;

namespace IllusionFixes
{
    [BepInProcess(Constants.GameProcessName)]
    //[BepInProcess(Constants.GameProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    [DefaultExecutionOrder(-1000)]
    public class MainGameOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KKS_Fix_MainGameOptimizations";
        public const string PluginName = "Main Game Optimizations";

        public static ConfigEntry<bool> AsyncClothesLoading { get; private set; }
        public static ConfigEntry<bool> PreloadCharacters { get; private set; }
        public static ConfigEntry<bool> ThrottleCharaUpdates { get; private set; }
        public static ConfigEntry<bool> ThrottleDynamicBoneUpdates { get; private set; }
        public static ConfigEntry<int> ThrottleDynamicBoneUpdatesRange { get; private set; }

        internal void Start()
        {

            AsyncClothesLoading = Config.Bind(Utilities.ConfigSectionTweaks, "Async clothes loading", true, new ConfigDescription("Spread loading of clothes in school roam mode over multiple frames. Greatly reduces seemingly random stutters when characters change clothes somewhere in the world.\nWarning: In rare cases can cause some visual glitches like 2 coordinates loaded at once."));
            PreloadCharacters = Config.Bind(Utilities.ConfigSectionTweaks, "Preload characters on initial load", true, new ConfigDescription("Forces all characters to load during initial load into school mode. Slightly longer loading time but eliminates large stutters when unseen characters enter current map."));
            ThrottleCharaUpdates = Config.Bind(Utilities.ConfigSectionTweaks, "Throttle chara blend shape updates", true, new ConfigDescription("Reduces the amount of unnecessary blend shape updates. Performance improvement in main game, especially with over 20 characters in one room."));
            ThrottleDynamicBoneUpdates = Config.Bind(Utilities.ConfigSectionTweaks, "Throttle dynamic bone updates", true, new ConfigDescription("Stops dynamic bone physics in roaming mode for characters that are far away or not visible. Performance improvement in main game, especially with over 20 characters.\nWarning: In rare cases can cause some physics glitches."));
            ThrottleDynamicBoneUpdatesRange = Config.Bind(Utilities.ConfigSectionTweaks, "Pause dynamic bones outside range", 16, new ConfigDescription("Stops dynamic bone physics in roaming mode for characters that are further than this many units away from player (1 unit is roughly 1m).\nLower value will greatly increase FPS, but skirts might look glitchy as characters move away.\nNeeds 'Throttle dynamic bone updates' to be enabled.", new AcceptableValueRange<int>(5, 25)));
            void UpdateThrottleDynamicBoneUpdatesRange() => _dynamicBoneUpdateSqrMaxDistance = ThrottleDynamicBoneUpdatesRange.Value * ThrottleDynamicBoneUpdatesRange.Value;
            UpdateThrottleDynamicBoneUpdatesRange();
            ThrottleDynamicBoneUpdatesRange.SettingChanged += (sender, args) => UpdateThrottleDynamicBoneUpdatesRange();

            Harmony.CreateAndPatchAll(typeof(MainGameOptimizations));

            SceneManager.sceneLoaded += (arg0, mode) =>
            {
                try
                {
                    _runningReloadCoroutines.Clear();
                    _insideRoamingMode = arg0.name == "Action" || Scene.LoadSceneName == "Action";
                    foreach (var boneListItem in _boneList.Keys.ToList())
                    {
                        if (!boneListItem)
                            _boneList.Remove(boneListItem);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            };
        }

        ///// <summary>
        ///// Prevent a crash that can break level loading and softlock the game
        ///// </summary>
        //[HarmonyFinalizer, HarmonyPatch(typeof(AI), "CoordinateSetting")]
        //public static Exception CoordinateSettingCatchException(AI __instance, Exception __exception)
        //{
        //    if (__exception != null)
        //    {
        //        UnityEngine.Debug.LogException(__exception);
        //        // Attempt to clean up state
        //        var h = __instance.heroine;
        //        if (h != null)
        //        {
        //            h.coordinates = new[] { 0 };
        //            h.isDresses = new[] { false };
        //        }
        //    }
        //
        //    return null;
        //}

        #region Optimize dynamic bones

        private sealed class BoneListItem
        {
            private readonly ChaControl _chara;
            private bool _lastState = true;

            public BoneListItem(ChaControl chara)
            {
                _chara = chara;
            }

            private readonly Dictionary<Component, Transform> _originalroots = new Dictionary<Component, Transform>();

            public void SetState(bool isVisible)
            {
                if (_lastState == isVisible) return;
                if (Scene.IsNowLoadingFade) return;

                _lastState = isVisible;

                foreach (var bone in _chara.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (!bone) continue;

                    // Setting bone.enabled to false breaks it so we have to hack around by setting Root to null which effectively disables the bones
                    switch (bone)
                    {
                        case DynamicBone x:
                            if (isVisible)
                            {
                                if (x.m_Root == null && _originalroots.TryGetValue(x, out var r)) x.m_Root = r;
                            }
                            else
                            {
                                if (x.m_Root != null)
                                {
                                    _originalroots[x] = x.m_Root;
                                    x.m_Root = null;
                                }
                            }

                            break;
                        case DynamicBone_Ver01 x:
                            if (isVisible)
                            {
                                if (x.m_Root == null && _originalroots.TryGetValue(x, out var r)) x.m_Root = r;
                            }
                            else
                            {
                                if (x.m_Root != null)
                                {
                                    _originalroots[x] = x.m_Root;
                                    x.m_Root = null;
                                }
                            }
                            break;
                        case DynamicBone_Ver02 x:
                            if (isVisible)
                            {
                                if (x.Root == null && _originalroots.TryGetValue(x, out var r)) x.Root = r;
                            }
                            else
                            {
                                if (x.Root != null)
                                {
                                    _originalroots[x] = x.Root;
                                    x.Root = null;
                                }
                            }
                            break;
                    }
                }
            }
        }

        private static readonly Dictionary<ChaControl, BoneListItem> _boneList = new Dictionary<ChaControl, BoneListItem>();
        private static Transform _playerTransform;
        private static Camera _camera;
        private static bool _insideRoamingMode;
        private static int _dynamicBoneUpdateSqrMaxDistance = 16 * 16;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateForce))]
        private static void DynamicBoneOptimize(ChaControl __instance)
        {
            // Only run in roaming mode. Includes roaming mode H
            if (_insideRoamingMode && ThrottleDynamicBoneUpdates.Value && __instance.loadEnd)
            {
                var isVisible = __instance.rendBody.isVisible && CheckDistance(__instance.transform.position);

                if (!_boneList.TryGetValue(__instance, out var boneListItem))
                {
                    boneListItem = new BoneListItem(__instance);
                    _boneList[__instance] = boneListItem;
                }

                boneListItem.SetState(isVisible);
            }
        }

        private static bool CheckDistance(Vector3 transformPosition)
        {
            if (_camera == null)
            {
                if (Camera.main == null || Camera.main.transform == null)
                    return true;
                _camera = Camera.main;
                _playerTransform = Camera.main.transform;
            }

            // pause bones of characters not visible on game screen
            var viewPos = _camera.WorldToViewportPoint(transformPosition);
            var isVisible = viewPos.z > 0 && viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1;
            if (!isVisible) return false;
            
            // pause bones of characters a certain distance away from camera
            var sqrDistMagnitude = (_playerTransform.position - transformPosition).sqrMagnitude;
            return sqrDistMagnitude < _dynamicBoneUpdateSqrMaxDistance;
        }

        #endregion

        #region Async clothes load

        private static readonly List<ChaControl> _runningReloadCoroutines = new List<ChaControl>();

        /// <summary>
        /// If characters change clothes anywhere, new clothes get loaded synchronously by default, causing 
        /// seemingly random stutter. Use async loading instead to mitigate this
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(NPC), nameof(NPC.SynchroCoordinate))]
        public static bool SynchroCoordinateOverride(NPC __instance, bool isRemove)
        {
            if (!AsyncClothesLoading.Value) return true;

            // Load immediately during load screens, better compat and slightly faster overall
            if (Scene.IsNowLoadingFade) return true;

            if (__instance.chaCtrl == null || __instance.heroine == null || !Character.IsInstance()) return false;

            try
            {
                var nowCoordinate = __instance.heroine.NowCoordinate;
                if (__instance.chaCtrl.fileStatus.coordinateType == nowCoordinate) return false;

                if (!__instance.chaCtrl.ChangeCoordinateType((ChaFileDefine.CoordinateType)nowCoordinate)) return false;

                Character.enableCharaLoadGCClear = false;
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
                                    Character.enableCharaLoadGCClear = true;

                                __instance.Pause(false);
                            });

                __instance.chaCtrl.StartCoroutine(reloadCoroutine);

                // Needed to counter SetActiveTop(false) at the start of ReloadAsync. That code is before 1st yield so 
                // it is executed in the current "thread" before returning to this call after 1st yield is encountered
                __instance.chaCtrl.SetActiveTop(isActive);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                // Clean up state
                _runningReloadCoroutines.Remove(__instance.chaCtrl);
                _runningReloadCoroutines.RemoveAll(c => c == null);
                if (_runningReloadCoroutines.Count == 0 && Singleton<Character>.IsInstance())
                    Character.enableCharaLoadGCClear = true;
            }

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

            var any = false;
            foreach (var codeInstruction in instructions)
            {
                if (codeInstruction.operand as MethodInfo == target)
                {
                    codeInstruction.opcode = OpCodes.Call;
                    codeInstruction.operand = replacement;
                    any = true;
                }
                yield return codeInstruction;
            }

            if (!any) throw new Exception("Didn't find target");
        }

        public static void ForcedSetActive(Base instance, bool isPop)
        {
            if (PreloadCharacters.Value)
            {
                var origPos = instance.position;
                if (Game.Player != null && Game.Player.transform)
                    instance.position = Game.Player.transform.position;

                // SetActive(true) that will always load the character, even if it's not on current map. Need to hide it after.
                instance.SetActive(true);
                if (!isPop)
                    instance.SetActive(false);

                instance.StartCoroutine(ForceSetPositionCo(instance, origPos));
                IEnumerator ForceSetPositionCo(Base i, Vector3 pos)
                {
                    i.position = pos;
                    // Need to wait to reset the position in some cases to prevent glitchyness
                    yield return new WaitForEndOfFrame();
                    i.position = pos;
                    yield return new WaitForEndOfFrame();
                    i.position = pos;
                }
            }
            else
            {
                instance.SetActive(isPop);
            }
        }

        #endregion

        #region Reduce blend shape update spam

        private static readonly Queue<ChaControl> _chaUpdateStatusQueue = new Queue<ChaControl>();
        private static readonly List<ChaControl> _throttledChaControls = new List<ChaControl>();
        private static Vector3? _playerPos;
        private static bool _needsFullCharaUpdate;

        private void Update()
        {
            // Turn off if can't determine the current game state or if turning it on will not bring any improvement
            if (!ThrottleCharaUpdates.Value ||
                // Only run in main game
                !ActionScene.initialized ||
                // less than 5 characters will degrade perf
                Game.HeroineList.Count < 6 ||
                // Fixes breaking mouths in invite events
                ActionScene.instance.isEventNow || Scene.IsNowLoadingFade || ADV.Program.isADVProcessing)
            {
                _needsFullCharaUpdate = true;
                return;
            }

            // Always run all updates in talk / h scenes since the number of characters is limited
            // Action == roaming mode, talk scene is loaded into AddSceneName
            if (!_insideRoamingMode || !string.IsNullOrEmpty(Scene.AddSceneName))
            {
                _needsFullCharaUpdate = true;
                return;
            }

            _needsFullCharaUpdate = false;
            _playerPos = null;
            _throttledChaControls.Clear();

            // Cache player position for the face updates since they get triggered many many more times every frame
            if (Game.Player != null && Game.Player.transform != null)
                _playerPos = Game.Player.transform.position;

            // Throttle down character updates to max 4 per frame (and turn them off during loading, above)
            if (Character.IsInstance())
            {
                var allCharaEntries = Character.chaControls;
                for (int i = 0; i < 4 && _chaUpdateStatusQueue.Count > 0; i++)
                {
                    var value = _chaUpdateStatusQueue.Dequeue();
                    // Make sure the character didn't get removed before we got to it
                    if (value && allCharaEntries.Contains(value))
                        _throttledChaControls.Add(value);
                }

                if (_chaUpdateStatusQueue.Count == 0)
                {
                    foreach (var value in allCharaEntries)
                        _chaUpdateStatusQueue.Enqueue(value);
                }
            }
        }

        private static List<ChaControl> GetThrottledChaControls()
        {
            return _needsFullCharaUpdate ? Character.ChaControls : _throttledChaControls;
        }

        private static IEnumerable<CodeInstruction> PatchCharaDic(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Character), nameof(Character.chaControls))))
                .ThrowIfInvalid("Character.chaControls not found")
                .SetOperandAndAdvance(AccessTools.Method(typeof(MainGameOptimizations), nameof(MainGameOptimizations.GetThrottledChaControls))).Instructions();
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(Character), nameof(Character.Update))]
        public static IEnumerable<CodeInstruction> CharacterUpdate(IEnumerable<CodeInstruction> instructions)
        {
            return PatchCharaDic(instructions);
        }

        [HarmonyTranspiler, HarmonyPatch(typeof(Character), nameof(Character.LateUpdate))]
        public static IEnumerable<CodeInstruction> CharacterLateUpdate(IEnumerable<CodeInstruction> instructions)
        {
            return PatchCharaDic(instructions);
        }

        /// <summary>
        /// Only update faces of characters near the player
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(FaceBlendShape), nameof(FaceBlendShape.LateUpdate))]
        public static bool FaceBlendShapeLateUpdate(FaceBlendShape __instance)
        {
            if (_needsFullCharaUpdate)
                return true;
            // Don't update during loading screens
            if (_throttledChaControls.Count == 0)
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
