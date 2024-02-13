#if KK || KKS

using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Studio;
using UnityEngine.UI;


namespace IllusionFixes
{
    /// <summary>
    /// Fixes a problem in the UI for loading scenes where page and scrollbar positions are not saved.
    /// This seems to occur only on KK/KKS.
    /// </summary>
    public class PageKeeper : MonoBehaviour
    {
        static float _scrollValue = 1f;
        static int _page = 0;

        Scrollbar _target;

#if KK
        int _startFrame;
        SceneLoadScene _parent;
#endif

        public static void Setup()
        {
            Harmony.CreateAndPatchAll(typeof(PageKeeper), FixSceneLoad.GUID + " PageKeeper");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SceneLoadScene), "Awake")]
        internal static void PostSceneLoadSceneAwake(SceneLoadScene __instance)
        {
            var gboj = __instance?.gameObject;
            if (gboj != null)
                gboj.AddComponent<PageKeeper>();
        }

        void Start()
        {
            _target = gameObject.GetComponentInChildren<Scrollbar>();

            if (_target == null)
            {
                Destroy(this);
                return;
            }

            _target.value = _scrollValue;

#if KK
            _startFrame = Time.frameCount;
            _parent = GetComponent<SceneLoadScene>();
#endif
        }

#if KK
        void LateUpdate()
        {
            //The page is reset by someone else, so set
            if (_parent != null && Time.frameCount - _startFrame <= 2)
                _parent.SetPage(_page);
        }
#endif

        private void OnDisable()
        {
            if (_target != null)
                _scrollValue = Mathf.Max(_target.value, 0.0001f);   //If it is 0, it is reset. So I won't set it to 0.

#if KK
            if (_parent != null)
                _page = SceneLoadScene.page;
#endif
        }
    }
}


#endif