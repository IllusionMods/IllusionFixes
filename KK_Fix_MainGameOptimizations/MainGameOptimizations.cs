using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ActionGame.Chara;
using BepInEx;
using Common;
using Harmony;
using Manager;

namespace KK_Fix_MainGameOptimizations
{
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class MainGameOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_MainGameOptimizations";
        public const string PluginName = "Main Game Optimizations";

        private void Awake()
        {
            HarmonyInstance.Create(GUID).PatchAll(typeof(MainGameOptimizations));
        }

        /// <summary>
        /// If characters change clothes anywhere, new clothes get loaded synchronously 
        /// by default, causing seemingly random stutter.
        /// Use async loading instead if characters are not visible to mitigate this 
        /// (can't use async load if character is visible because of visual bugs)
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(NPC), nameof(NPC.SynchroCoordinate))]
        public static bool SynchroCoordinateOverride(NPC __instance, bool isRemove)
        {
            // Fall back to original loading mode when character is visible to avoid visual bugs
            if (__instance.isActive)
                return true;

            // If not visible, do async loading of the clothes instead, replaces the original method
            if (__instance.chaCtrl == null || !Character.IsInstance())
                return false;

            var nowCoordinate = __instance.heroine.NowCoordinate;
            if (__instance.chaCtrl.fileStatus.coordinateType == nowCoordinate) return false;

            if (!__instance.chaCtrl.ChangeCoordinateType((ChaFileDefine.CoordinateType)nowCoordinate)) return false;

            __instance.chaCtrl.StartCoroutine(
                Utils.ComposeCoroutine(
                    Utils.CreateCoroutine(() => Singleton<Character>.Instance.enableCharaLoadGCClear = false),
                    __instance.chaCtrl.ReloadAsync(false, true, true, true, true),
                    Utils.CreateCoroutine(() =>
                    {
                        if (isRemove)
                            __instance.chaCtrl.RandomChangeOfClothesLowPoly(__instance.heroine.lewdness);
                        Singleton<Character>.Instance.enableCharaLoadGCClear = true;
                    })
                ));

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

        /// <summary>
        /// SetActive that will always load the character, even if it's initially disabled
        /// </summary>
        public static void ForcedSetActive(Base instance, bool isPop)
        {
            instance.SetActive(true);
            if (!isPop)
                instance.SetActive(false);
        }
    }
}
