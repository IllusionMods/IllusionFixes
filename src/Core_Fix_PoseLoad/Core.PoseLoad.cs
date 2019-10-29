using BepInEx.Harmony;
using HarmonyLib;
using Studio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IllusionFixes
{
    public partial class PoseLoad
    {
        public const string PluginName = "Pose Load Fix";

        internal void Start() => HarmonyWrapper.PatchAll(typeof(Hooks));

        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl.FileInfo), nameof(PauseCtrl.FileInfo.Apply))]
            internal static bool Apply(PauseCtrl.FileInfo __instance, OCIChar _char)
            {
                //AI and KK pose files are apparently indistinguishable from each other
                //If the user is holding ctrl while loading the pose treat it as a KK pose for the purposes of correcting the right hand
                bool KKPose = false;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    KKPose = true;

                //326 is a bone that exists in HS but not KK, check that to see if this is a loaded HS pose
                bool HSPose = __instance.dicFK.Keys.Any(x => x == 326);

                if (!HSPose && !KKPose) return true;

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
                        //Breasts translated from HS to KK
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
#if KK
                        //Correct the right hand
                        if (key == 22 || key == 25 || key == 28 || key == 31 || key == 34)
                            item2.Value.rot = new Vector3(-item2.Value.rot.x, 180 + item2.Value.rot.y, 180 - item2.Value.rot.z);

                        if (key == 23 || key == 26 || key == 29 || key == 32 || key == 35)
                            item2.Value.rot = new Vector3(item2.Value.rot.x, -item2.Value.rot.y, -item2.Value.rot.z);

                        if (key == 24 || key == 27 || key == 30 || key == 33 || key == 36)
                            item2.Value.rot = new Vector3(item2.Value.rot.x, -item2.Value.rot.y, -item2.Value.rot.z);
#elif AI
                        if (KKPose && !HSPose)
                        {
                            //Correct the right hand
                            if (key == 22 || key == 25 || key == 28 || key == 31 || key == 34)
                                item2.Value.rot = new Vector3(-item2.Value.rot.x, 180 + item2.Value.rot.y, 180 - item2.Value.rot.z);

                            if (key == 23 || key == 26 || key == 29 || key == 32 || key == 35)
                                item2.Value.rot = new Vector3(item2.Value.rot.x, -item2.Value.rot.y, -item2.Value.rot.z);

                            if (key == 24 || key == 27 || key == 30 || key == 33 || key == 36)
                                item2.Value.rot = new Vector3(item2.Value.rot.x, -item2.Value.rot.y, -item2.Value.rot.z);
                        }
#endif

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
