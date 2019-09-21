using Common;
using HarmonyLib;
using System;
using System.IO;

namespace IllusionFixes
{
    public partial class ManifestCorrector
    {
        internal static class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), typeof(string), typeof(string), typeof(Type), typeof(string))]
            internal static void LoadAssetPrefix(ref string assetName, string assetBundleName, ref string manifestAssetBundleName)
            {
                if (!manifestAssetBundleName.IsNullOrEmpty() && !File.Exists($"abdata/{manifestAssetBundleName}"))
                {
                    if (manifestAssetBundleName == "abdata")
                        Utilities.Logger.LogError($"abdata file not found in abdata folder, probable corrupt game install");
                    else
                    {
                        Utilities.Logger.LogDebug($"Corrected manifestAssetBundleName for assetName:{assetName} assetBundleName:{assetBundleName} manifestAssetBundleName:{manifestAssetBundleName} Method:LoadAsset");
                        manifestAssetBundleName = "";
                    }
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAssetAsync), typeof(string), typeof(string), typeof(Type), typeof(string))]
            internal static void LoadAssetAsyncPrefix(ref string assetName, string assetBundleName, ref string manifestAssetBundleName)
            {
                if (!manifestAssetBundleName.IsNullOrEmpty() && !File.Exists($"abdata/{manifestAssetBundleName}"))
                {
                    if (manifestAssetBundleName == "abdata")
                        Utilities.Logger.LogError($"abdata file not found in abdata folder, probable corrupt game install");
                    else
                    {
                        Utilities.Logger.LogDebug($"Corrected manifestAssetBundleName for assetName:{assetName} assetBundleName:{assetBundleName} manifestAssetBundleName:{manifestAssetBundleName} Method:LoadAssetAsync");
                        manifestAssetBundleName = "";
                    }
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.GetLoadedAssetBundle))]
            internal static void GetLoadedAssetBundlePrefix(string assetBundleName, ref string manifestAssetBundleName)
            {
                if (!manifestAssetBundleName.IsNullOrEmpty() && !File.Exists($"abdata/{manifestAssetBundleName}"))
                {
                    if (manifestAssetBundleName == "abdata")
                        Utilities.Logger.LogError($"abdata file not found in abdata folder, probable corrupt game install");
                    else
                    {
                        Utilities.Logger.LogDebug($"Corrected manifestAssetBundleName for assetBundleName:{assetBundleName} manifestAssetBundleName:{manifestAssetBundleName} Method:GetLoadedAssetBundle");
                        manifestAssetBundleName = "";
                    }
                }
            }
        }
    }
}
