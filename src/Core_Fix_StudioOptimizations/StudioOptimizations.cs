using BepInEx;
using HarmonyLib;
using Studio;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
#if AI || HS2
using AIChara;
#endif

namespace IllusionFixes
{
    public partial class StudioOptimizations : BaseUnityPlugin
    {
        public const string GUID = "Fix_StudioOptimizations";
        public const string PluginName = "Studio Optimizations";

        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(StudioOptimizations), GUID);
            MeasuringLoadTimes.Setup(base.Logger);
        }

        /// <summary>
        /// Points where accessories and Studio items are attached
        /// </summary>
#if AI || HS2
        public static HashSet<string> AccessoryAttachPoints = new HashSet<string> { "N_Waist", "N_Waist_f", "N_Waist_b", "N_Waist_L", "N_Waist_R", "N_Ana", "N_Kokan", "N_Knee_L", "N_Foot_L", "N_Ankle_L", "N_Leg_L", "N_Knee_R", "N_Foot_R", "N_Ankle_R", "N_Leg_R", "N_Dan", "N_Tikubi_L", "N_Tikubi_R", "N_Mouth", "N_Earring_L", "N_Earring_R", "N_Hitai", "N_Head", "N_Head_top", "N_Hair_pin_R", "N_Hair_pin_L", "N_Hair_twin_R", "N_Hair_twin_L", "N_Hair_pony", "N_Nose", "N_Megane", "N_Face", "N_Neck", "N_Elbo_L", "N_Index_L", "N_Middle_L", "N_Ring_L", "N_Hand_L", "N_Wrist_L", "N_Arm_L", "N_Shoulder_L", "N_Elbo_R", "N_Index_R", "N_Middle_R", "N_Ring_R", "N_Hand_R", "N_Wrist_R", "N_Arm_R", "N_Shoulder_R", "N_Chest", "N_Back", "N_Back_R", "N_Back_L", "N_Chest_f" };
#else
        public static HashSet<string> AccessoryAttachPoints = new HashSet<string> { "a_n_nip_L", "a_n_nip_R", "a_n_shoulder_L", "a_n_arm_L", "a_n_wrist_L", "a_n_hand_L", "a_n_ind_L", "a_n_mid_L", "a_n_ring_L", "a_n_elbo_L", "a_n_shoulder_R", "a_n_arm_R", "a_n_wrist_R", "a_n_hand_R", "a_n_ind_R", "a_n_mid_R", "a_n_ring_R", "a_n_elbo_R", "a_n_mouth", "a_n_hair_pin_R", "a_n_hair_pin", "a_n_hair_pony", "a_n_hair_twin_L", "a_n_hair_twin_R", "a_n_head", "a_n_headflont", "a_n_headside", "a_n_headtop", "a_n_earrings_L", "a_n_earrings_R", "a_n_nose", "a_n_megane", "a_n_neck", "a_n_back", "a_n_back_L", "a_n_back_R", "a_n_bust", "a_n_bust_f", "a_n_ana", "a_n_kokan", "a_n_dan", "a_n_leg_L", "a_n_ankle_L", "a_n_heel_L", "a_n_knee_L", "a_n_leg_R", "a_n_ankle_R", "a_n_heel_R", "a_n_knee_R", "a_n_waist", "a_n_waist_b", "a_n_waist_f", "a_n_waist_L", "a_n_waist_R" };
#endif
        /// <summary>
        /// Map from the name of transform to path where it resides
        /// </summary>
        private static Dictionary<string, List<string>> NameToPathMap = new Dictionary<string, List<string>>();

        //Variables to avoid GC
        private static List<string> _transformPaths = new List<string>();
        private static StringBuilder _pathBuilder = new StringBuilder();

        /// <summary>
        /// FindLoop but doesn't search through accessories
        /// </summary>
        public static GameObject FindLoopNoAcc(Transform transform, string findName)
        {
            if (NameToPathMap.TryGetValue(findName, out var pathList))
            {
                for( int i = 0, n = pathList.Count; i < n; ++i )
                {
                    var child = transform.Find(pathList[i]);
                    if (child != null)
                        return child.gameObject;
                }
            }

            List<string> paths = _transformPaths;
            paths.Clear();

            var gobj = FindLoopNoAccWithPaths(transform, findName, paths);

            if (gobj != null)
            {
                if (pathList == null)
                    pathList = NameToPathMap[findName] = new List<string>();

                var builder = _pathBuilder;
                builder.Length = 0;

                builder.Append(paths[paths.Count - 1]);
                for (int i = paths.Count - 2; i >= 0; --i)
                {
                    builder.Append('/');
                    builder.Append(paths[i]);
                }

                pathList.Add(builder.ToString());
            }

            return gobj;
        }

        private static GameObject FindLoopNoAccWithPaths(Transform transform, string findName, List<string> paths)
        {
            string transformName = transform.name;
            if (string.CompareOrdinal(findName, transformName) == 0)   
                return transform.gameObject;

            if (AccessoryAttachPoints.Contains(transformName))
                return null;

            for (int i = 0, childCount = transform.childCount; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                GameObject gameObject = FindLoopNoAccWithPaths(child, findName, paths);

                if (gameObject != null)
                {
                    paths.Add(child.name);
                    return gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Same as above but returns a Transform (HS2)
        /// </summary>
        public static Transform FindLoopNoAccTransform(Transform transform, string name)
        {
            if (string.CompareOrdinal(name, transform.gameObject.name) == 0)
                return transform;

            if (AccessoryAttachPoints.Contains(transform.name))
                return null;

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject gameObject = FindLoopNoAcc(transform.GetChild(i), name);
                if (gameObject != null)
                    return gameObject.transform;
            }

            return null;
        }

        //Prevent certain methods from searching through accessory hierarchy, if these methods find body transform names within accessories it breaks everything
        //This is done by replacing calls to FindLoop with calls to a similar method that doesn't search accessories
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FKCtrl), nameof(FKCtrl.InitBones))]
        [HarmonyPatch(typeof(AddObjectAssist), nameof(AddObjectAssist.InitBone))]
        [HarmonyPatch(typeof(AddObjectAssist), nameof(AddObjectAssist.InitHairBone))]
        private static IEnumerable<CodeInstruction> InitBoneTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            for (var index = 0; index < instructionsList.Count; index++)
            {
                var x = instructionsList[index];
                if (x.operand?.ToString() == "UnityEngine.GameObject FindLoop(UnityEngine.Transform, System.String)")
                    x.operand = typeof(StudioOptimizations).GetMethod(nameof(FindLoopNoAcc), AccessTools.all);
                if (x.operand?.ToString() == "UnityEngine.Transform FindLoop(UnityEngine.Transform, System.String)")
                    x.operand = typeof(StudioOptimizations).GetMethod(nameof(FindLoopNoAccTransform), AccessTools.all);
            }

            return instructionsList;
        }

        /// <summary>
        /// Fix not being able to replace a character if an FK or IK node is selected
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), nameof(CharaList.ChangeCharaFemale))]
        private static bool CharaList_ChangeCharaFemale(CharaFileSort ___charaFileSort)
        {
            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
            for (int i = 0; i < selectNodes.Length; i++)
                if (Studio.Studio.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    if (objectCtrlInfo is OCICharFemale ociChar)
                        ociChar.ChangeChara(___charaFileSort.selectPath);
            return false;
        }

        /// <summary>
        /// Fix not being able to replace a character if an FK or IK node is selected
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(CharaList), nameof(CharaList.ChangeCharaMale))]
        private static bool CharaList_ChangeCharaMale(CharaFileSort ___charaFileSort)
        {
            TreeNodeObject[] selectNodes = Singleton<Studio.Studio>.Instance.treeNodeCtrl.selectNodes;
            for (int i = 0; i < selectNodes.Length; i++)
                if (Studio.Studio.Instance.dicInfo.TryGetValue(selectNodes[i], out ObjectCtrlInfo objectCtrlInfo))
                    if (objectCtrlInfo is OCICharMale ociChar)
                        ociChar.ChangeChara(___charaFileSort.selectPath);
            return false;
        }

        #region Fix attaching charas to other charas
        /// <summary>
        /// Fixes crashes when adding guide objects if the objects already exist for some reason
        /// Happens when replacing characters parented to other characters
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GuideObjectManager), nameof(GuideObjectManager.Add), typeof(Transform), typeof(int))]
        private static void GuideObjectManagerAddPatch(GuideObjectManager __instance, Transform _target, int _dicKey, Dictionary<Transform, GuideObject> ___dicGuideObject, Dictionary<Transform, Light> ___dicTransLight)
        {
            if (___dicGuideObject.TryGetValue(_target, out var existing))
            {
                Destroy(existing);
                ___dicGuideObject.Remove(_target);
            }
            if (___dicTransLight.TryGetValue(_target, out var existing2))
            {
                Destroy(existing2);
                ___dicTransLight.Remove(_target);
            }
        }

        /// <summary>
        /// Fixes nullref exception spam after removing a character attached to another character in some cases.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(FKCtrl), "LateUpdate")]
        private static Exception FKCtrlLateUpdateCleanupFinalizer(FKCtrl __instance, NullReferenceException __exception)
        {
            if (__exception == null) return null;

            Common.Utilities.Logger.LogInfo("Cleaning up destroyed objects from FKCtrl.listBones");

            var boneList = __instance.listBones;

            foreach (var targetInfo in boneList.ToList())
            {
                if (targetInfo?.gameObject == null)
                    boneList.Remove(targetInfo);
            }

            __instance.count = boneList.Count;

            return null;
        }
        #endregion

        #region Fast studio startup

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

#if HS2 || KKS
            Manager.Scene.LoadReserve(new Manager.Scene.Data
            {
                levelName = "Studio",
                // Turn off fading in to save more startup time
                isFade = false
            }, false);
#else
            Manager.Scene.Instance.LoadReserve(new Manager.Scene.Data
            {
                levelName = "Studio",
                // Turn off fading in to save more startup time
                isFade = false
            }, false);
#endif
        }

        private static void RunCoroutineImmediately(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is IEnumerator en)
                    RunCoroutineImmediately(en);
            }
        }

        #endregion

        /// <summary>
        /// Stop studio character and outfit lists from running UnloadUnusedAssets and GC.Collect unnecessarily on mouse hover.
        /// Instead, only destroy the previous thumbnail texture which basically has the same effect but is instant.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.LoadCharaImage))]
        [HarmonyPatch(typeof(MPCharCtrl.CostumeInfo), nameof(MPCharCtrl.CostumeInfo.LoadImage))]
        private static IEnumerable<CodeInstruction> StudioListLagFixTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(false, 
                              new CodeMatch(OpCodes.Ldfld), // <- this is the RawImage field, it changes between the two methods
                              new CodeMatch(OpCodes.Ldloc_0),
                              new CodeMatch(OpCodes.Ldfld),
                              new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PngAssist), nameof(PngAssist.LoadTexture))))
                .ThrowIfInvalid("LoadTexture target field not found")
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Dup)) // Duplicate value of the RawImage field and use it in our new method call
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StudioOptimizations), nameof(StudioOptimizations.DisposeRawTexture))))
                .MatchForward(false,
                              new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Resources), nameof(Resources.UnloadUnusedAssets))),
                              new CodeMatch(OpCodes.Pop))
                .ThrowIfInvalid("UnloadUnusedAssets not found")
                .SetOpcodeAndAdvance(OpCodes.Nop) // Nop out the UnloadUnusedAssets call and the Pop of its return value. Keep the operand in case other transpliers look for it.
                .SetOpcodeAndAdvance(OpCodes.Nop)
                .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GC), nameof(GC.Collect), new Type[0])))
                .ThrowIfInvalid("GC.Collect not found")
                .SetOpcodeAndAdvance(OpCodes.Nop) // Nop out as above, except there's no return value.
                .Instructions();
        }
        private static void DisposeRawTexture(RawImage target)
        {
            Destroy(target.texture);
        }
    }
}
