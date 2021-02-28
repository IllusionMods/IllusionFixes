using System;
using ActionGame;
using ActionGame.Chara;
using HarmonyLib;
using Illusion.Component;
using KKAPI.Utilities;
using Manager;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace IllusionFixes
{
    public partial class RestoreMissingFunctions
    {
        private static class CalendarIconHooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ActionMap), "Reserve")]
            private static void OnMapChangedHook(ActionMap __instance)
            {
                if (__instance.mapRoot == null || __instance.isMapLoading) return;

                // All 4 classroom ids
                if (__instance.no == 5 || __instance.no == 8 || __instance.no == 9 || __instance.no == 11)
                {
                    try
                    {
                        SpawnCrestActionPoint();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }

            private static void SpawnCrestActionPoint()
            {
                Logger.LogDebug("Spawning calendar icon action point");

                if (_iconOff == null)
                {
                    _iconOff = (ResourceUtils.GetEmbeddedResource(@"action_icon_calendar_off.png").LoadTexture()
                                ?? throw new Exception("asset not found - action_icon_calendar_off")).ToSprite();
                    DontDestroyOnLoad(_iconOff);
                }

                if (_iconOn == null)
                {
                    _iconOn = (ResourceUtils.GetEmbeddedResource(@"action_icon_calendar_on.png").LoadTexture()
                               ?? throw new Exception("asset not found - action_icon_calendar_on")).ToSprite();
                    DontDestroyOnLoad(_iconOn);
                }

                var inst = CommonLib.LoadAsset<GameObject>("map/playeractionpoint/00.unity3d", "PlayerActionPoint_05", true);
                var parent = GameObject.Find("Map/ActionPoints");
                inst.transform.SetParent(parent.transform, true);

                var pap = inst.GetComponentInChildren<PlayerActionPoint>();
                var iconRootObject = pap.gameObject;
                var iconRootTransform = pap.transform;
                DestroyImmediate(pap, false);

                // position above the small table
                iconRootTransform.position = new Vector3(2.43f, 0.45f, 4.55f);

                var evt = iconRootObject.AddComponent<TriggerEnterExitEvent>();
                var animator = iconRootObject.GetComponentInChildren<Animator>();
                var rendererIcon = iconRootObject.GetComponentInChildren<SpriteRenderer>();
                rendererIcon.sprite = _iconOff;
                var playerInRange = false;
                evt.onTriggerEnter += c =>
                {
                    if (!c.CompareTag("Player")) return;
                    playerInRange = true;
                    animator.Play("icon_action");
                    rendererIcon.sprite = _iconOn;
                    c.GetComponent<Player>().actionPointList.Add(evt);
                };
                evt.onTriggerExit += c =>
                {
                    if (!c.CompareTag("Player")) return;
                    playerInRange = false;
                    animator.Play("icon_stop");
                    rendererIcon.sprite = _iconOff;
                    c.GetComponent<Player>().actionPointList.Remove(evt);
                };

                var player = Singleton<Game>.Instance.actScene.Player;
                evt.UpdateAsObservable()
                    .Subscribe(_ =>
                    {
                        // Hide in H scenes and other places
                        var isVisible = Singleton<Game>.IsInstance() && !Singleton<Game>.Instance.IsRegulate(true);
                        if (rendererIcon.enabled != isVisible) 
                            rendererIcon.enabled = isVisible;

                        // Check if player clicked this point
                        if (isVisible && playerInRange && ActionInput.isAction && !player.isActionNow && !Singleton<Scene>.Instance.IsNowLoadingFade)
                        {
                            Singleton<Scene>.Instance.LoadReserve(new Scene.Data
                            {
                                assetBundleName = "action/menu/classschedulemenu.unity3d",
                                levelName = "ClassScheduleMenu",
                                isAdd = true,
                                isAsync = true
                            }, false);
                        }
                    })
                    .AddTo(evt);
            }

            private static Sprite _iconOff, _iconOn;
        }
    }
}