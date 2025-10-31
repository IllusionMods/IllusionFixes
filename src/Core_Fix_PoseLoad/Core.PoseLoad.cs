using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Common;
using ExtensibleSaveFormat;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace IllusionFixes
{
    [BepInProcess(Constants.StudioProcessName)]
    [BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
    [BepInPlugin(GUID, PluginName, Constants.PluginsVersion)]
    public class PoseLoad : BaseUnityPlugin
    {
        public const string PluginName = "Pose Load Fix";
        public const string GUID = "Fix_PoseLoad";
        private static ExtendedSave.GameNames PoseGameName;

        internal void Start()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));

            ExtendedSave.PoseBeingLoaded += ExtendedSave_PoseBeingLoaded;
        }

        private void ExtendedSave_PoseBeingLoaded(string poseName, PauseCtrl.FileInfo fileInfo, OCIChar ociChar, ExtendedSave.GameNames gameName)
        {
            PoseGameName = gameName;
        }

        /// <summary>
        /// Use of TryGetValue prevents errors when loading HS poses and poses from different gender, so this method replaces the vanilla method
        /// </summary>
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl.FileInfo), nameof(PauseCtrl.FileInfo.Apply))]
            internal static bool PauseCtrl_FileInfo_Apply(PauseCtrl.FileInfo __instance, OCIChar _char)
            {
                //AI and KK pose files are apparently indistinguishable from each other
                //If the user is holding ctrl while loading the pose correct the right hand FK
                bool correctHand = false;

                if (PoseGameName != ExtendedSave.GameNames.Unknown)
                {
#if KK || KKS
                    if (PoseGameName != ExtendedSave.GameNames.Koikatsu && PoseGameName != ExtendedSave.GameNames.KoikatsuSunshine)
                        correctHand = true;
#elif AI|| HS2
                    if (PoseGameName == ExtendedSave.GameNames.Koikatsu || PoseGameName == ExtendedSave.GameNames.KoikatsuSunshine)
                        correctHand = true;
#endif
                }

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    correctHand = true;

                //326 is a bone that exists in HS but not KK, check that to see if this is a loaded HS pose
                bool HSPose = __instance.dicFK.Keys.Any(x => x == 326);
#if KK || KKS
                //Honey Select poses always need the right hand corrected in KK
                if (HSPose)
                    correctHand = true;
#endif

                #region Vanilla Code
                _char.LoadAnime(__instance.group, __instance.category, __instance.no, __instance.normalizedTime);

                for (int i = 0; i < __instance.activeIK.Length; i++)
                    _char.ActiveIK((OIBoneInfo.BoneGroup)(1 << i), __instance.activeIK[i]);
                _char.ActiveKinematicMode(OICharInfo.KinematicMode.IK, __instance.enableIK, _force: true);
                foreach (KeyValuePair<int, ChangeAmount> item in __instance.dicIK)
                    _char.oiCharInfo.ikTarget[item.Key].changeAmount.Copy(item.Value);

                for (int j = 0; j < __instance.activeFK.Length; j++)
                    _char.ActiveFK(FKCtrl.parts[j], __instance.activeFK[j]);
                _char.ActiveKinematicMode(OICharInfo.KinematicMode.FK, __instance.enableFK, _force: true);
                #endregion

                foreach (KeyValuePair<int, ChangeAmount> item2 in __instance.dicFK)
                {
                    int key = item2.Key;

                    if (HSPose)
                    {
                        //Breasts translated from HS to KK/AI
                        if (key == 326) key = 53;
                        if (key == 327) key = 54;
                        if (key == 328) key = 55;
                        if (key == 329) key = 56;
                        if (key == 330) key = 57;

                        if (key == 332) key = 59;
                        if (key == 333) key = 60;
                        if (key == 334) key = 61;
                        if (key == 335) key = 62;
                        if (key == 336) key = 63;
                    }

                    if (_char.oiCharInfo.bones.TryGetValue(key, out var oIBoneInfo))
                    {
                        if (correctHand)
                        {
                            //Correct the right hand
                            if (key == 22 || key == 25 || key == 28 || key == 31 || key == 34)
                                item2.Value.rot = new Vector3(-item2.Value.rot.x, 180 + item2.Value.rot.y, 180 - item2.Value.rot.z).TrimRotation();

                            if (key == 23 || key == 26 || key == 29 || key == 32 || key == 35)
                                item2.Value.rot = new Vector3(item2.Value.rot.x, -item2.Value.rot.y, -item2.Value.rot.z).TrimRotation();

                            if (key == 24 || key == 27 || key == 30 || key == 33 || key == 36)
                                item2.Value.rot = new Vector3(item2.Value.rot.x, -item2.Value.rot.y, -item2.Value.rot.z).TrimRotation();

                            //Adjust the thumbs on both hands
#if KK || KKS
                            if (key == 37 || key == 22)
                                item2.Value.rot = (Quaternion.Euler(item2.Value.rot) * Quaternion.Euler(90, 10, -20)).eulerAngles.TrimRotation();

                            if (key == 38 || key == 23)
                                item2.Value.rot = (Quaternion.Euler(0, 0, -item2.Value.rot.y) * Quaternion.Euler(0, 0, 30)).eulerAngles.TrimRotation();

                            if (key == 39 || key == 24)
                                item2.Value.rot = new Vector3(0, 0, -item2.Value.rot.y).TrimRotation();
#elif AI || HS2
                            if (key == 37 || key == 22)
                                item2.Value.rot = (Quaternion.Euler(item2.Value.rot) * Quaternion.Euler(-90, -20, 10)).eulerAngles.TrimRotation();

                            if (key == 38 || key == 23)
                                item2.Value.rot = (Quaternion.Euler(0, -item2.Value.rot.z, 0) * Quaternion.Euler(0, 30, 0)).eulerAngles.TrimRotation();

                            if (key == 39 || key == 24)
                                item2.Value.rot = new Vector3(0, -item2.Value.rot.z, 0).TrimRotation();
#endif
                        }

                        oIBoneInfo.changeAmount.Copy(item2.Value);
                    }
                }

                #region Vanilla Code
                for (int k = 0; k < __instance.expression.Length; k++)
                    _char.EnableExpressionCategory(k, __instance.expression[k]);
                #endregion

                return false;
            }
        }
    }
}

internal static class Extensions
{
    /// <summary>
    /// Trim the values of the vector down to 0-360 range
    /// </summary>
    public static Vector3 TrimRotation(this Vector3 vector)
    {
        vector.x %= 360f;
        vector.y %= 360f;
        vector.z %= 360f;
        return vector;
    }
}