using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Common;
using HarmonyLib;
using KKAPI;
using KKAPI.Maker;

#if AI || HS2
using AIChara;
#endif

namespace IllusionFixes
{
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class NullChecks
    {
        internal const string PluginName = "Null Checks";

        private static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            var h = Harmony.CreateAndPatchAll(typeof(Hooks));

            // Catch nullref exceptions inside of Change***Async methods to prevent bad clothes completely crashing game logic
            // Finalizer only affects the start of the coroutines while postfix affects the proceeding steps
            var finalizer = new HarmonyMethod(typeof(NullChecks), nameof(MethodNullRefEater));
            var postfix = new HarmonyMethod(typeof(NullChecks), nameof(CoroutineNullRefEater));
            foreach (var methodInfo in typeof(ChaControl).GetMethods(AccessTools.all)
                .Where(x => x.Name.StartsWith("Change", StringComparison.Ordinal) && x.Name.EndsWith("Async", StringComparison.Ordinal)))
            {
                if (methodInfo.ReturnType == typeof(IEnumerator))
                    h.Patch(methodInfo, postfix: postfix, finalizer: finalizer);
                else
                    h.Patch(methodInfo, finalizer: finalizer);
            }

            // Catch crashes when loading corrupted coordinate files
            // Find the Stream overload of LoadFile, it's different across games
            var target = typeof(ChaFileCoordinate).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                                  .FirstOrDefault(x => x.Name == nameof(ChaFileCoordinate.LoadFile) && x.GetParameters().FirstOrDefault()?.ParameterType == typeof(Stream));
            if (target == null)
                throw new InvalidOperationException("Failed to find ChaFileCoordinate.LoadFile");
            h.Patch(target, finalizer: finalizer);
        }

        private static void ChaFileCoordinate_LoadFile_CrashEater(Stream st, ref Exception __exception, ref bool __result)
        {
            if (__exception != null)
            {
                Logger.LogWarning($"Caught an unexpected crash while trying to read a coordinate from {(st is FileStream fs ? fs.Name : st?.GetType().Name)}\n{__exception}");
                __exception = null;
                __result = false;
            }
        }

        private static void MethodNullRefEater(ref Exception __exception)
        {
            if (__exception != null)
            {
                Logger.LogError("Swallowing exception to prevent game crash!\n" + __exception);
                __exception = null;
            }
        }

        private static void CoroutineNullRefEater(ref IEnumerator __result)
        {
            IEnumerator EatExceptionWrapper(IEnumerator original)
            {
                while (true)
                {
                    var hasNext = false;
                    try
                    {
                        hasNext = original.MoveNext();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Swallowing exception to prevent game crash!\n" + e);
                    }

                    if (hasNext)
                        yield return original.Current;
                    else
                        yield break;
                }
            }

            if (__result != null)
                __result = EatExceptionWrapper(__result);
        }

        private static class Hooks
        {
            [HarmonyFinalizer, HarmonyPatch(typeof(Illusion.Game.Utils.Bundle), nameof(Illusion.Game.Utils.Bundle.LoadSprite))]
            private static Exception LoadSpriteNullRefEater(Exception __exception, string assetBundleName, string assetName)
            {
                if (__exception != null)
                {
                    Logger.LogError($"Caught a crash when trying to load sprite {assetBundleName} > {assetName}! \n{__exception}");
                }

                return null;
            }

            [HarmonyFinalizer, HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.ResetParticlesPosition))]
            internal static Exception ParticlesCrashCatcher(Exception __exception, DynamicBone __instance, ref UnityEngine.Vector3 ___m_ObjectPrevPosition)
            {
                if (__exception != null)
                {
                    // Prevent state from getting corrupted
                    ___m_ObjectPrevPosition = __instance.transform.position;
                    Logger.LogError("Swallowing exception to prevent game crash!\n" + __exception);
                }
                return null;
            }

            [HarmonyFinalizer, HarmonyPatch(typeof(DynamicBone), nameof(DynamicBone.UpdateDynamicBones))]
            internal static Exception ParticlesCrashCatcher2(Exception __exception, DynamicBone __instance)
            {
                if (__exception != null)
                {
                    if (__exception is NullReferenceException && __instance.m_Particles.Any(x => x.m_Transform == null))
                    {
                        if (__instance.m_Root != null)
                        {
                            Logger.LogWarning("Attempting to fix invalid DynamicBone particles!");
                            __instance.SetupParticles();
                            __instance.m_Particles.RemoveAll(x => x.m_Transform == null);
                        }
                        else
                        {
                            Logger.LogWarning("Invalid DynamicBone particles caused a crash, but m_Root is null so they can't be fixed.");
                        }
                    }
                    else
                    {
                        Logger.LogError("Swallowing exception to prevent game crash!\n" + __exception);
                    }
                }
                return null;
            }

#if !EC
            //Fix for errors resulting from Studio objects with no ItemComponent
            [HarmonyPrefix, HarmonyPatch(typeof(Studio.OCIItem), nameof(Studio.OCIItem.SetPatternTex), typeof(int), typeof(int))]
            internal static bool OCIItemSetPatternTex(int _key, Studio.OCIItem __instance)
            {
                if (__instance.itemComponent == null && _key > 0)
                {
                    Logger.LogError($"ItemComponent is null itemInfo.no:{__instance.itemInfo?.no}");
                    return false;
                }

                return true;
            }
#endif

#if KK || EC || KKS
            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairColor))]
            internal static void ChangeSettingHairColor(int parts, ChaControl __instance) => RemoveNullParts(__instance.GetCustomHairComponent(parts));

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairOutlineColor))]
            internal static void ChangeSettingHairOutlineColor(int parts, ChaControl __instance) => RemoveNullParts(__instance.GetCustomHairComponent(parts));

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeSettingHairAcsColor))]
            internal static void ChangeSettingHairAcsColor(int parts, ChaControl __instance) => RemoveNullParts(__instance.GetCustomHairComponent(parts));

            [HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.UpdateAccessoryMoveFromInfo))]
            internal static void UpdateAccessoryMoveFromInfo(int slotNo, ChaControl __instance) => RemoveNullParts(__instance.GetAccessoryObject(slotNo)?.GetComponent<ChaCustomHairComponent>());

            private static void RemoveNullParts(ChaCustomHairComponent hairComponent)
            {
                if (hairComponent == null)
                    return;

                hairComponent.rendAccessory = hairComponent.rendAccessory.RemoveNulls();
                hairComponent.rendHair = hairComponent.rendHair.RemoveNulls();
                hairComponent.trfLength = hairComponent.trfLength.RemoveNulls();
            }
#endif

#if KK || KKS
            [HarmonyFinalizer, HarmonyPatch(typeof(SetRenderQueue_Custom), nameof(SetRenderQueue_Custom.ChangeRendererQueue))]
            private static Exception ChangeRendererQueueErrorHandler(Exception __exception, SetRenderQueue_Custom __instance)
            {
                // ChangeRendererQueue is called from Update after an item is loaded on every frame until it doesn't crash.
                // If m_queueDatas has bad data in it then this will result in exception spam on every frame.
                // This eats the exception so that there is only one log entry for the exception and the component can usually work just fine.
                if (__exception != null)
                {
                    Logger.LogError($"Caught a crash in ChangeRendererQueue! This studio item might not work correctly until SetRenderQueue_Custom.m_queueDatas is fixed in the mod! Please report this issue to the mod author.\n" +
                                    $"Path: {StrayTech.GameObjectExtension.FullPath(__instance)}\n" +
                                    $"Exception: {__exception}");
                }
                return null;
            }
#endif

#if AI || HS2 || KKS
            /// <summary>
            /// RuntimeUtilities.GetAllAssemblyTypes is bugged - if some types can't be loaded it either skips the whole assembly or causes a nullref on every frame later on if the .GetType doesn't throw but does return a null in the type array.
            /// </summary>
            [HarmonyPrefix, HarmonyPatch(typeof(UnityEngine.Rendering.PostProcessing.RuntimeUtilities), nameof(UnityEngine.Rendering.PostProcessing.RuntimeUtilities.GetAllAssemblyTypes))]
            internal static void FixedGetAllAssemblyTypes()
            {
                if (UnityEngine.Rendering.PostProcessing.RuntimeUtilities.m_AssemblyTypes == null)
                {
                    // todo: AccessTools.AllTypes in current HarmonyX ver. has the same issue of returning a null sometimes on assemblies with unloadable types but that don't crash .GetType somehow
                    UnityEngine.Rendering.PostProcessing.RuntimeUtilities.m_AssemblyTypes = AccessTools.AllTypes().Where(x => x != null).ToArray();
                }
            }
#endif
        }
    }
}
