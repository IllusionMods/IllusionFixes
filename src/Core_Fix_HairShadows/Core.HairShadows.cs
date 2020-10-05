using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IllusionFixes
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class HairShadowsFix : BaseUnityPlugin
    {
        public const string GUID = "Fix_HairShadows";
        public const string PluginName = "Hair Shadows Fix";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        private const int HairRenderQueue = 2475;

        internal void Main()
        {
            Logger = base.Logger;

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        internal static class Hooks
        {
            /// <summary>
            /// Set the render queue for front hairs down so that they receive shadows
            /// </summary>
            /// <param name="actObj">Object being loaded</param>
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "LoadCharaFbxDataAsync")]
            internal static void LoadCharaFbxDataAsync(ref Action<GameObject> actObj)
            {
                Action<GameObject> oldAct = actObj;
                actObj = delegate (GameObject o)
                {
                    oldAct(o);
                    if (o == null)
                        return;
                    var hair = o.GetComponent<ChaCustomHairComponent>();
                    if (hair != null)
                    {
                        foreach (var rend in o.GetComponentsInChildren<Renderer>())
                        {
                            foreach (var mat in rend.materials)
                                if (mat.shader.name == "Shader Forge/main_hair_front")
                                    mat.renderQueue = HairRenderQueue;
                        }
                    }
                };
            }
            /// <summary>
            /// Change the render queue for eyes and eyebrows visible through hair setting to compensate for lower front hair render queue
            /// </summary>

            [HarmonyPostfix, HarmonyPatch(typeof(SetRenderQueue_Custom), "Awake")]
            internal static void SetRenderQueue_CustomAwake(Renderer ___rend, ref SetRenderQueue_Custom.QueueData[] ___m_queueDatas)
            {
                switch (___rend.name)
                {
                    case "cf_O_mayuge":
                        ___m_queueDatas[0].m_queues = HairRenderQueue - 1;
                        break;
                    case "cf_O_eyeline":
                        ___m_queueDatas[0].m_queues = HairRenderQueue - 1;

                        SetRenderQueue_Custom.QueueData kage = new SetRenderQueue_Custom.QueueData();
                        kage.id = 1;
                        kage.m_queues = HairRenderQueue - 2;
                        kage.m_queuesBackup = 2902;

                        List<SetRenderQueue_Custom.QueueData> newQueueData = new List<SetRenderQueue_Custom.QueueData>();
                        newQueueData.Add(___m_queueDatas[0]);
                        newQueueData.Add(kage);
                        ___m_queueDatas = newQueueData.ToArray();
                        break;
                    case "cf_O_eyeline_low":
                        ___m_queueDatas[0].m_queues = HairRenderQueue - 1;
                        break;
                    case "cf_Ohitomi_L":
                    case "cf_Ohitomi_R":
                        ___m_queueDatas[0].m_queues = HairRenderQueue - 3;
                        break;
                    case "cf_Ohitomi_L02":
                    case "cf_Ohitomi_R02":
                        ___m_queueDatas[0].m_queues = HairRenderQueue - 1;
                        break;
                }
            }
        }
    }
}