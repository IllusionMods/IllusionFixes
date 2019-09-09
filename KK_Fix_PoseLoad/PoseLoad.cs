using BepInEx;
using BepInEx.Harmony;
using Common;
using HarmonyLib;
using Studio;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IllusionFixes
{
    /// <summary>
    /// Corrects Honey Select poses loaded in Koikatsu
    /// </summary>
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public partial class PoseLoad : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_PoseLoad";
        public const string PluginName = "Pose Load Fix";

        internal void Start() => HarmonyWrapper.PatchAll(typeof(Hooks));

        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(PauseCtrl.FileInfo), nameof(PauseCtrl.FileInfo.Apply))]
            internal static bool Apply(PauseCtrl.FileInfo __instance, OCIChar _char)
            {
                bool HS = __instance.dicFK.Keys.Any(x => x == 326);
                if (!HS) return true;

                _char.LoadAnime(__instance.group, __instance.category, __instance.no, __instance.normalizedTime);

                for (int i = 0; i < __instance.activeIK.Length; i++)
                    _char.ActiveIK((OIBoneInfo.BoneGroup)(1 << i), __instance.activeIK[i]);
                _char.ActiveKinematicMode(OICharInfo.KinematicMode.IK, __instance.enableIK, _force: true);
                foreach (KeyValuePair<int, ChangeAmount> item in __instance.dicIK)
                    _char.oiCharInfo.ikTarget[item.Key].changeAmount.Copy(item.Value);

                for (int j = 0; j < __instance.activeFK.Length; j++)
                    _char.ActiveFK(FKCtrl.parts[j], __instance.activeFK[j]);
                _char.ActiveKinematicMode(OICharInfo.KinematicMode.FK, __instance.enableFK, _force: true);

                foreach (KeyValuePair<int, ChangeAmount> item2 in __instance.dicFK)
                {
                    int key = item2.Key;

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

                    if (_char.oiCharInfo.bones.TryGetValue(key, out var oIBoneInfo))
                    {
                        //Correct the right hand
                        if (key == 22 || key == 25 || key == 28 || key == 31 || key == 34)
                            item2.Value.rot = new Vector3(-item2.Value.rot.x, 180 + item2.Value.rot.y, 180 - item2.Value.rot.z);

                        if (key == 23 || key == 26 || key == 29 || key == 32 || key == 35)
                            item2.Value.rot = new Vector3(item2.Value.rot.x, -item2.Value.rot.y, -item2.Value.rot.z);

                        if (key == 24 || key == 27 || key == 30 || key == 33 || key == 36)
                            item2.Value.rot = new Vector3(item2.Value.rot.x, -item2.Value.rot.y, -item2.Value.rot.z);

                        oIBoneInfo.changeAmount.Copy(item2.Value);
                    }
                }

                for (int k = 0; k < __instance.expression.Length; k++)
                    _char.EnableExpressionCategory(k, __instance.expression[k]);

                return false;
            }
        }
    }
}
