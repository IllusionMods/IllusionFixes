using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Common;
using Harmony;
using Studio;
using UnityEngine;
using Logger = BepInEx.Logger;

namespace KK_Fix_ResourceUnloadOptimizations
{
    /// <summary>
    /// Changes any invalid personalities to the "Pure" personality to prevent the game from breaking when adding them to the class
    /// </summary>
    [BepInPlugin(GUID, PluginName, Metadata.PluginsVersion)]
    public class ResourceUnloadOptimizations : BaseUnityPlugin
    {
        public const string GUID = "KK_Fix_ResourceUnloadOptimizations";
        public const string PluginName = "Resource Unload Optimizations";

        private static ResourceUnloadOptimizations _instance;

        private void Awake()
        {
            _instance = this;

            var hi = HarmonyInstance.Create(GUID);
            var tpl = new HarmonyMethod(AccessTools.Method(typeof(ResourceUnloadOptimizations), nameof(ReplaceUnloadAssetsTpl)));

            var targetMethods = new List<MethodInfo>();
            // Faster tab switching
            targetMethods.Add(AccessTools.Method(typeof(SceneLoadScene), "SetPage"));
            // Faster scene load / item manipulation
            var ociTargets = new[] { "SetMainTex", "SetPatternTex" };
            targetMethods.AddRange(AccessTools.GetDeclaredMethods(typeof(OCIItem)).Where(x => x.GetParameters().Length > 0 && ociTargets.Contains(x.Name)));
            // Faster scene load
            targetMethods.Add(AccessTools.Method(typeof(FrameCtrl), nameof(FrameCtrl.Load)));
            targetMethods.Add(AccessTools.Method(typeof(FrameCtrl), nameof(FrameCtrl.Release)));
            targetMethods.Add(AccessTools.Method(typeof(BackgroundCtrl), nameof(BackgroundCtrl.Load)));

            // Faster chara list load
            targetMethods.Add(AccessTools.Method(typeof(CharaList), "LoadCharaImage"));

            foreach (var targetMethod in targetMethods)
                hi.Patch(targetMethod, null, null, tpl);
        }

        public static IEnumerable<CodeInstruction> ReplaceUnloadAssetsTpl(IEnumerable<CodeInstruction> instructions)
        {
            var target = AccessTools.Method(typeof(Resources), nameof(Resources.UnloadUnusedAssets));
            var replacement = AccessTools.Method(typeof(ResourceUnloadOptimizations), nameof(ScheduleUnloadUnusedAssets));

            if (target == null) throw new ArgumentNullException(nameof(target));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));

            foreach (var instruction in instructions)
            {
                if (Equals(instruction.operand, target))
                {
                    Logger.Log(LogLevel.Info, "ReplaceUnloadAssetsTpl OK");
                    instruction.operand = replacement;
                }

                yield return instruction;
            }
        }

        public static AsyncOperation ScheduleUnloadUnusedAssets()
        {
            // Reset delay timer every call
            _instance.CancelInvoke(nameof(RunUnloadUnusedAssets));
            _instance.Invoke(nameof(RunUnloadUnusedAssets), 5f);
            return null;
        }

        private void RunUnloadUnusedAssets()
        {
            Logger.Log(LogLevel.Info, "RunUnloadUnusedAssets");
            Resources.UnloadUnusedAssets();
        }
    }
}
