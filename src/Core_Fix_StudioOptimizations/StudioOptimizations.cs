using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Harmony;
using HarmonyLib;
using Manager;
using Studio;
using UnityEngine;

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

        #region Fix attaching charas to other charas

        /// <summary>
        /// Points where accessories and Studio items are attached
        /// </summary>
#if AI || HS2
        public static HashSet<string> AccessoryAttachPoints = new HashSet<string>() { "N_Waist", "N_Waist_f", "N_Waist_b", "N_Waist_L", "N_Waist_R", "N_Ana", "N_Kokan", "N_Knee_L", "N_Foot_L", "N_Ankle_L", "N_Leg_L", "N_Knee_R", "N_Foot_R", "N_Ankle_R", "N_Leg_R", "N_Dan", "N_Tikubi_L", "N_Tikubi_R", "N_Mouth", "N_Earring_L", "N_Earring_R", "N_Hitai", "N_Head", "N_Head_top", "N_Hair_pin_R", "N_Hair_pin_L", "N_Hair_twin_R", "N_Hair_twin_L", "N_Hair_pony", "N_Nose", "N_Megane", "N_Face", "N_Neck", "N_Elbo_L", "N_Index_L", "N_Middle_L", "N_Ring_L", "N_Hand_L", "N_Wrist_L", "N_Arm_L", "N_Shoulder_L", "N_Elbo_R", "N_Index_R", "N_Middle_R", "N_Ring_R", "N_Hand_R", "N_Wrist_R", "N_Arm_R", "N_Shoulder_R", "N_Chest", "N_Back", "N_Back_R", "N_Back_L", "N_Chest_f" };
#else
        public static HashSet<string> AccessoryAttachPoints = new HashSet<string>() { "a_n_nip_L", "a_n_nip_R", "a_n_shoulder_L", "a_n_arm_L", "a_n_wrist_L", "a_n_hand_L", "a_n_ind_L", "a_n_mid_L", "a_n_ring_L", "a_n_elbo_L", "a_n_shoulder_R", "a_n_arm_R", "a_n_wrist_R", "a_n_hand_R", "a_n_ind_R", "a_n_mid_R", "a_n_ring_R", "a_n_elbo_R", "a_n_mouth", "a_n_hair_pin_R", "a_n_hair_pin", "a_n_hair_pony", "a_n_hair_twin_L", "a_n_hair_twin_R", "a_n_head", "a_n_headflont", "a_n_headside", "a_n_headtop", "a_n_earrings_L", "a_n_earrings_R", "a_n_nose", "a_n_megane", "a_n_neck", "a_n_back", "a_n_back_L", "a_n_back_R", "a_n_bust", "a_n_bust_f", "a_n_ana", "a_n_kokan", "a_n_dan", "a_n_leg_L", "a_n_ankle_L", "a_n_heel_L", "a_n_knee_L", "a_n_leg_R", "a_n_ankle_R", "a_n_heel_R", "a_n_knee_R", "a_n_waist", "a_n_waist_b", "a_n_waist_f", "a_n_waist_L", "a_n_waist_R" };
#endif

        /// <summary>
        /// Prevent FindAll from also finding objects attached as accessories that can potentially override the character's own objects that we actually want.
        /// Fixes some bugs when attaching a character to another character in studio.
        /// </summary>
        [HarmonyPrefix]
        //private void FindAll(Transform trf)
        [HarmonyPatch(typeof(FindAssist), "FindAll", typeof(Transform))]
        private static void FindAllPatch(FindAssist __instance, Transform trf)
        {
            if (!__instance.dictObjName.ContainsKey(trf.name))
                __instance.dictObjName[trf.name] = trf.gameObject;

            if (AccessoryAttachPoints.Contains(trf.name) && trf.parent.gameObject.name != "ct_hairB")
                return;

            for (var i = 0; i < trf.childCount; i++)
                FindAllPatch(__instance, trf.GetChild(i));
        }

        /// <summary>
        /// Fixes crashes when adding guide objects if the objects already exist for some reason
        /// Happens when replacing characters parented to other characters
        /// </summary>
        [HarmonyPrefix]
        //Studio.GuideObjectManager.Add(Transform _target, int _dicKey) : GuideObject
        [HarmonyPatch(typeof(GuideObjectManager), nameof(GuideObjectManager.Add), typeof(Transform), typeof(int))]
        private static void FindAllPatch(GuideObjectManager __instance, Transform _target, int _dicKey, Dictionary<Transform, GuideObject> ___dicGuideObject, Dictionary<Transform, Light> ___dicTransLight)
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

            var fk = Traverse.Create(__instance);
            var boneList = fk.Field("listBones").GetValue<IList>();

            foreach (var targetInfo in boneList.Cast<object>().ToList())
            {
                if (Traverse.Create(targetInfo).Field("gameObject").GetValue<GameObject>() == null)
                    boneList.Remove(targetInfo);
            }

            fk.Property("count").SetValue(boneList.Count);

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

#if HS2
            Scene.LoadReserve(new Scene.Data
            {
                levelName = "Studio",
                // Turn off fading in to save more startup time
                isFade = false
            }, false);
#else
            Scene.Instance.LoadReserve(new Scene.Data
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
    }
}
