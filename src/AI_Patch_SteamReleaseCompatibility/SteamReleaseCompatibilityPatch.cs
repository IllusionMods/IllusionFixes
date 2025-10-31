using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil;

namespace AI_Fixes
{
    public static class SteamReleaseCompatibilityPatch
    {
        private static ManualLogSource _logger;
        private static ManualLogSource Logger { get => _logger ?? (_logger = BepInEx.Logging.Logger.CreateLogSource(nameof(SteamReleaseCompatibilityPatch))); }

        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

        public static void Patch(AssemblyDefinition mainAss)
        {
            // Don't apply at all to the japanese version
            if (Paths.ProcessName == "AI-Syoujyo" ||
                !File.Exists(Path.Combine(Paths.GameRootPath, "AI-Shoujo.exe"))) return;

            // Fix breaking body models in studio
            FixBodyAssets();

            if (Paths.ProcessName == "AI-Shoujo")
            {
                // Patch studio classes into the main game exe to maintain compatibility with the japanese release
                FixMissingStudioClasses(mainAss);

                // Make plugins marked to run only for AI-Syoujyo.exe to run
                AccessTools.Property(typeof(Paths), nameof(Paths.ProcessName)).GetSetMethod(true).Invoke(null, new object[] { "AI-Syoujyo" });
            }
        }

        private static void FixBodyAssets()
        {
            // When in studio, it needs the full body asset files, but main game still needs the split limited asset files
            var isStudio = Paths.ProcessName == "StudioNEOV2";

            var mmBase = new FileInfo(Path.Combine(Paths.GameRootPath, @"abdata\chara\mm_base.unity3d"));
            var mmBaseOrig = new FileInfo(mmBase.FullName + ".original");
            if (!mmBaseOrig.Exists) mmBase.CopyTo(mmBaseOrig.FullName, true);

            var ooBase = new FileInfo(Path.Combine(Paths.GameRootPath, @"abdata\chara\oo_base.unity3d"));
            var ooBaseOrig = new FileInfo(ooBase.FullName + ".original");
            if (!ooBaseOrig.Exists) ooBase.CopyTo(ooBaseOrig.FullName, true);

            if (isStudio)
            {
                var mmBaseStu = new FileInfo(Path.Combine(Paths.GameRootPath, @"abdata\chara\mm_base_studio.unity3d"));
                if (mmBaseStu.Exists)
                {
                    mmBaseStu.CopyTo(mmBase.FullName, true);
                    Logger.LogInfo($"Replacing {mmBase.Name} with {mmBaseStu.Name} to fix Studio compatibility");
                }
                else
                {
                    Logger.Log(LogLevel.Warning | LogLevel.Message, $"Could not find {mmBaseStu.Name} to fix Studio compatibility, expect bugs!");
                }

                var ooBaseStu = new FileInfo(Path.Combine(Paths.GameRootPath, @"abdata\chara\oo_base_studio.unity3d"));
                if (ooBaseStu.Exists)
                {
                    ooBaseStu.CopyTo(ooBase.FullName, true);
                    Logger.LogInfo($"Replacing {ooBase.Name} with {ooBaseStu.Name} to fix Studio compatibility");
                }
                else
                {
                    Logger.Log(LogLevel.Warning | LogLevel.Message, $"Could not find {ooBaseStu.Name} to fix Studio compatibility, expect bugs!");
                }
            }
            else
            {
                mmBaseOrig.CopyTo(mmBase.FullName, true);
                ooBaseOrig.CopyTo(ooBase.FullName, true);
                Logger.LogDebug($"Restoring original {mmBase.Name} and {ooBase.Name}");
            }
        }

        private static void FixMissingStudioClasses(AssemblyDefinition mainAss)
        {
            var studioAssPath = Path.Combine(Paths.GameRootPath, @"StudioNEOV2_Data\Managed\Assembly-CSharp.dll");

            if (!File.Exists(studioAssPath))
            {
                Logger.Log(LogLevel.Warning | LogLevel.Message, "Cannot apply compatibility patches because studio is not installed. Expect plugin crashes!");
                return;
            }

            var sw = Stopwatch.StartNew();

            // Create a copy of studio assembly with a different name to not conflict with main game assembly
            var studioAss = AssemblyDefinition.ReadAssembly(studioAssPath);
            string newStudioAssemblyName = "Studio-CSharp";
            studioAss.Name.Name = newStudioAssemblyName;
            studioAss.MainModule.Name = $"{newStudioAssemblyName}.dll";

            var mainAllTypes = mainAss.MainModule.GetTypes();
            var mainTypesLookup = new HashSet<string>(mainAllTypes.Select(x => x.FullName));

            var studioAllTypes = studioAss.MainModule.GetTypes();
            var studioAllTypesGrouped = studioAllTypes.GroupBy(x => mainTypesLookup.Contains(x.FullName));
            var studioTypesToRedirect = studioAllTypesGrouped.First(x => !x.Key);
            var studioTypesToDelete = studioAllTypesGrouped.First(x => x.Key);

            // Cross-reference the two assemblies
            var studioAssRef = new AssemblyNameReference(studioAss.Name.Name, studioAss.Name.Version);
            _mainAssRef = new AssemblyNameReference(mainAss.Name.Name, mainAss.Name.Version);
            mainAss.MainModule.AssemblyReferences.Add(studioAssRef);
            studioAss.MainModule.AssemblyReferences.Add(_mainAssRef);

            // Create a link in main game assembly to the missing studio types
            var forwardAttrConstructor = mainAss.MainModule.ImportReference(typeof(TypeForwardedToAttribute).GetConstructor(new[] { typeof(Type) }));
            foreach (var studioType in studioTypesToRedirect)
            {
                if (studioType.IsPublic)
                {
                    var forwardAttribute = new CustomAttribute(forwardAttrConstructor);
                    var t = mainAss.MainModule.ImportReference(studioType);
                    forwardAttribute.ConstructorArguments.Add(new CustomAttributeArgument(mainAss.MainModule.ImportReference(typeof(Type)), t));
                    mainAss.CustomAttributes.Add(forwardAttribute);
                    mainAss.MainModule.ExportedTypes.Add(new ExportedType(t.Namespace, t.Name, t.Module, t.Scope));
                }
            }

            // Preform voodoo to remove duplicate types in studio ass and instead use the types from the main assembly
            // Big thanks to Horse for sharing the voodoo knowledge
            foreach (var typeDefinition in studioTypesToDelete)
            {
                // Don't touch, can break
                if (typeDefinition.Name == "<Module>") continue;

                studioAss.MainModule.Types.Remove(typeDefinition);

                typeDefinition.Scope = _mainAssRef;
                typeDefinition.GetType().GetField("module", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(typeDefinition, studioAss.MainModule);
                typeDefinition.MetadataToken = new MetadataToken(TokenType.TypeRef, 0);

                foreach (var typeDefinitionMethod in typeDefinition.Methods.ToList())
                    typeDefinitionMethod.MetadataToken = new MetadataToken(TokenType.MemberRef, 0);

                foreach (var typeDefinitionField in typeDefinition.Fields.ToList())
                    typeDefinitionField.MetadataToken = new MetadataToken(TokenType.MemberRef, 0);
            }

            // Preform further voodoo with the harmony patches to make the above voodoo work
            var h = Harmony.CreateAndPatchAll(typeof(SteamReleaseCompatibilityPatch));
            byte[] outputAss = null;
            using (var ms = new MemoryStream())
            {
                studioAss.Write(ms, new WriterParameters() { WriteSymbols = false });
                outputAss = ms.ToArray();
            }
            h.UnpatchSelf();

            Assembly.Load(outputAss);

            studioAss.Dispose();

            Logger.LogDebug($"Finished patching main game assembly with missing studio classes in {sw.ElapsedMilliseconds}ms");

            // If assembly dumping is turned on, dump the generated studio assembly
            var s = (ConfigFile)typeof(ConfigFile).GetProperty("CoreConfig", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null, null);
            if (s != null && s.TryGetEntry<bool>("Preloader", "DumpAssemblies", out var entry) && entry.Value)
            {
                var studioDumpPath = Path.Combine(Paths.BepInExRootPath, "DumpedAssemblies", newStudioAssemblyName + ".dll");
                File.WriteAllBytes(studioDumpPath, outputAss);
            }
        }

        private static AssemblyNameReference _mainAssRef;

        [HarmonyPatch(typeof(TypeDefinition), "IsDefinition", MethodType.Getter)]
        [HarmonyPostfix]
        private static void IsDefinitionPatch(TypeDefinition __instance, ref bool __result)
        {
            if (__instance.Scope is AssemblyNameReference assRef && assRef.Name == _mainAssRef.Name)
                __result = false;
        }

        [HarmonyPatch(typeof(TypeDefinition), nameof(TypeDefinition.IsValueType), MethodType.Setter)]
        [HarmonyPrefix]
        private static bool IsValueTypePatch()
        {
            return false;
        }
    }
}