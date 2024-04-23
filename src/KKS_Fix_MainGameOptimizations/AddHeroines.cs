using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro; 

namespace IllusionFixes
{
    public partial class MainGameOptimizations : BaseUnityPlugin
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionGame.ClassRoomList), nameof(ActionGame.ClassRoomList.GetClassRegisterMax))]
        static private bool GetClassRegisterMaxPrefix(ActionGame.ClassRoomList __instance, ref int __result, int schoolClass )
        {
            __result = NewGetClassRegisterMax(schoolClass);
            return false;
        }

        static private int maxOffset = 20 + 25;     // Maximum number of people to be added with schoolClass==0

        static private int NewGetClassRegisterMax(int schoolClass)
        {
            int classRegisterMax = AddHeroines.Value;

            switch (schoolClass)
            {
                case 0:
                    classRegisterMax += 20;
                    break;
                case 1:
                    classRegisterMax += 5;
                    break;
                case 2:
                    classRegisterMax += 15;
                    break;
            }
            if (Manager.Game.saveData.isLiveNumMax)
            {
                switch (schoolClass)
                {
                    case 0:
                        classRegisterMax += 25;
                        break;
                    case 1:
                        classRegisterMax += 15;
                        break;
                    case 2:
                        classRegisterMax += 20;
                        break;
                }
            }
            return classRegisterMax;
        }

        /// <summary>
        /// Remove heroines that exceed the maximum limit.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(SaveData.WorldData), nameof(SaveData.WorldData.Save), typeof(string), typeof(string))]
        [HarmonyPriority(Priority.High)]
        static void WorldDataSavePrefix(SaveData.WorldData __instance, string path, string fileName)
        {
            var heroines = __instance.heroineList;

            int write = 0;

            for( int read = 0; read < heroines.Count; ++read )
            {
                var heroine = heroines[read];
                int schoolClass = heroine.schoolClass;

                if( heroine.schoolClassIndex < NewGetClassRegisterMax(schoolClass) )
                    heroines[write++] = heroine;
            }

            heroines.RemoveRange(write, heroines.Count - write);
        }

        /// <summary>
        /// Add a GUI frame for heroine display
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionGame.ClassRoomList), nameof(ActionGame.ClassRoomList.charaPreviewList), MethodType.Getter)]
        static private void CharaPreviewListPostfix(ActionGame.ClassRoomList __instance, ref List<ActionGame.PreviewClassData> __result)
        {
            if (__result == null || __instance._charaPreviewList == null )
                return;

            var list = __instance._charaPreviewList;
            int max = AddHeroines.Value + maxOffset;

            if (list.Count >= max)
                return;

            float[] xValues;
            float dy;
            {
                HashSet<float> xValueSet = new HashSet<float>(new RoundFloatComperer());
                HashSet<float> yValueSet = new HashSet<float>(new RoundFloatComperer());

                for (int i = 0; i < Math.Min(20, list.Count); ++i)
                {
                    var pos = list[i].transform.localPosition;
                    xValueSet.Add(pos.x);
                    yValueSet.Add(pos.y);
                }

                xValues = xValueSet.OrderBy(x => x).ToArray();

                var yValues = yValueSet.OrderBy(y => y).ToArray();
                dy = yValues[1] - yValues[0];
            }

            var last = list[list.Count - 1];
            var lastPos = last.transform.localPosition;
            int xIndex = 0;

            for (; xIndex < xValues.Length; ++xIndex)
            {
                if (Mathf.Approximately(lastPos.x, xValues[xIndex]))
                    break;
            }

            float newY = lastPos.y;

            while ( list.Count < max)
            {   
                var newItem = GameObject.Instantiate(last, last.transform.parent);
                newItem.name = "Seat_" + list.Count;

                ++xIndex;

                if (xIndex >= xValues.Length)
                {
                    xIndex = 0;
                    newY += dy;
                }

                var newPos = newItem.transform.localPosition;
                newPos.x = xValues[xIndex];
                newPos.y = newY;
                newItem.transform.localPosition = newPos;

                list.Add(newItem);
            }

            UpdateFontSize(__instance);
        }

        class RoundFloatComperer : IEqualityComparer<float>
        {
            static int Round( float x )
            {
                return Mathf.RoundToInt(x * 5f);
            }

            public bool Equals(float x, float y)
            {
                return Round(x) == Round(y);
            }

            public int GetHashCode(float obj)
            {
                return Round(obj).GetHashCode();
            }
        }

        static void UpdateFontSize(ActionGame.ClassRoomList crl)
        {
            var sumGObj = crl.transform.Find("NPCEdit/PreviewSeat/SumBG/Sum");

            if (sumGObj == null)
                return;

            var text = sumGObj.GetComponent<TMPro.TextMeshProUGUI>();

            if (text == null)
                return;

            text.fontSizeMin = text.fontSizeMax - 4;
            text.autoSizeTextContainer = true;
        }
    }
}
