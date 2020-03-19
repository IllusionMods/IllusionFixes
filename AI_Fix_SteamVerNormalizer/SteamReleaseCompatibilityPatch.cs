using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
            if (Paths.ProcessName == "AI-Syoujyo") return;

            // Fix breaking body models in studio and possibly in some mods
            FixBodyAssets();

            // Patch studio classes into the main game exe to maintain compatibility with the japanese release
            if (Paths.ProcessName == "AI-Shoujo")
            {
                FixMissingStudioClasses(mainAss);

                //var h = new Harmony(nameof(SteamReleaseCompatibilityPatch));
                //h.Patch(AccessTools.Property(typeof(Paths), nameof(Paths.ProcessName)).GetGetMethod(), new HarmonyMethod(typeof(SteamReleaseCompatibilityPatch), nameof(ProcessNamePatch)));

                AccessTools.Property(typeof(Paths), nameof(Paths.ProcessName)).GetSetMethod(true).Invoke(null, new object[] { "AI-Syoujyo" });
            }
        }

        //private static bool ProcessNamePatch(ref string __result)
        //{
        //    //new StackFrame(1).GetMethod()
        //    __result = "AI-Syoujyo";
        //    return false;
        //}


        //internal static void RandomizeCAB(byte[] assetBundleData)
        //{
        //    var ascii = Encoding.ASCII.GetString(assetBundleData, 0, Math.Min(1024, assetBundleData.Length - 4));
        //
        //    var origCabIndex = ascii.IndexOf("CAB-", StringComparison.Ordinal);
        //
        //    if (origCabIndex < 0)
        //        return;
        //
        //    assetBundleData[origCabIndex + 4] = (byte)'c';
        //    assetBundleData[origCabIndex + 5] = (byte)'o';
        //    assetBundleData[origCabIndex + 6] = (byte)'p';
        //    assetBundleData[origCabIndex + 7] = (byte)'y';
        //}

        private static void FixBodyAssets()
        {
            // todo only replace for studio, or patch studio
            //var mmBaseEx = new FileInfo(Path.Combine(Paths.GameRootPath, @"abdata\chara\mm_base_ex.unity3d"));
            //if (mmBaseEx.Exists)
            //{
            //    var mmBase = new FileInfo(Path.Combine(Paths.GameRootPath, @"abdata\chara\mm_base.unity3d"));
            //    if (!mmBase.Exists || mmBaseEx.Length != mmBase.Length)
            //    {
            //        Logger.LogInfo("Replacing mm_base.unity3d with mm_base_ex.unity3d to improve compatibility");
            //        var targetPath = mmBase.FullName;
            //        if (mmBase.Exists)
            //        {
            //            File.Delete(mmBase.FullName + ".bak");
            //            mmBase.MoveTo(mmBase.FullName + ".bak");
            //        }
            //        var newBytes = File.ReadAllBytes(mmBaseEx.FullName);
            //        RandomizeCAB(newBytes);
            //        File.WriteAllBytes(targetPath, newBytes);
            //    }
            //}
            //
            //var ooBaseEx = new FileInfo(Path.Combine(Paths.GameRootPath, @"abdata\chara\oo_base_ex.unity3d"));
            //if (ooBaseEx.Exists)
            //{
            //    var ooBase = new FileInfo(Path.Combine(Paths.GameRootPath, @"abdata\chara\oo_base.unity3d"));
            //    if (!ooBase.Exists || ooBaseEx.Length != ooBase.Length)
            //    {
            //        Logger.LogInfo("Replacing oo_base.unity3d with oo_base_ex.unity3d to improve compatibility");
            //        var targetPath = ooBase.FullName;
            //        if (ooBase.Exists)
            //        {
            //            File.Delete(ooBase.FullName + ".bak");
            //            ooBase.MoveTo(ooBase.FullName + ".bak");
            //        }
            //        var newBytes = File.ReadAllBytes(ooBaseEx.FullName);
            //        RandomizeCAB(newBytes);
            //        File.WriteAllBytes(targetPath, newBytes);
            //    }
            //}
        }

        private static void FixMissingStudioClasses(AssemblyDefinition mainAss)
        {
            var studioAssPath = Path.Combine(Paths.GameRootPath, @"StudioNEOV2_Data\Managed\Assembly-CSharp.dll");

            if (!File.Exists(studioAssPath))
            {
                Logger.Log(LogLevel.Warning | LogLevel.Message, "Cannot apply compatibility patches because studio is not installed. Expect plugin crashes!");
                return;
            }

            //TODO need to forward all classes that don't exist and remove all that do exist

            var sw = Stopwatch.StartNew();

            // Create a copy of studio assembly with a different name to not conflict with main game assembly
            var studioAss = AssemblyDefinition.ReadAssembly(studioAssPath);
            string newStudioAssemblyName = "Studio-CSharp";
            studioAss.Name.Name = newStudioAssemblyName;
            studioAss.MainModule.Name = newStudioAssemblyName;

            var studioAllTypes = studioAss.MainModule.GetTypes();
            var studioTypesToRedirect = studioAllTypes.Where(x => x.Namespace.StartsWith("Studio"));

            mainAss.MainModule.AssemblyReferences.Add(studioAss.Name);
            studioAss.MainModule.AssemblyReferences.Add(mainAss.Name);

            var forwardAttrConstructor = mainAss.MainModule.ImportReference(typeof(TypeForwardedToAttribute).GetConstructor(new[] { typeof(Type) }));

            foreach (var studioType in studioTypesToRedirect)
            {
                // Make the studio methods use types from main game assembly if any are available
                // Needed to make the method signature match with what the plugins expect
                foreach (var studioMethod in studioType.Methods)
                {
                    studioMethod.CustomAttributes.Clear();
                    ResolveMethod(studioMethod, studioAss, mainAss);
                }
                foreach (var studioProp in studioType.Properties)
                {
                    studioProp.CustomAttributes.Clear();
                    if (studioProp.GetMethod != null)
                        ResolveMethod(studioProp.GetMethod, studioAss, mainAss);
                    if (studioProp.SetMethod != null)
                        ResolveMethod(studioProp.SetMethod, studioAss, mainAss);
                    studioProp.PropertyType = ResolveReference(studioProp.PropertyType, studioAss, mainAss);
                }
                foreach (var studioField in studioType.Fields.ToList())
                {
                    studioField.CustomAttributes.Clear();
                    studioField.FieldType = ResolveReference(studioField.FieldType, studioAss, mainAss);
                }

                studioType.CustomAttributes.Clear();

                if (studioType.BaseType != null)
                    studioType.BaseType = ResolveReference(studioType.BaseType, studioAss, mainAss);

                //todo are nested classes handled properly? Do they get resolved by looking up the owning type?
                if (!studioType.IsNested)
                {
                    // Create a link in main game assembly to the missing studio types
                    var forwardAttribute = new CustomAttribute(forwardAttrConstructor);
                    forwardAttribute.ConstructorArguments.Add(new CustomAttributeArgument(mainAss.MainModule.ImportReference(typeof(Type)), studioType));
                    mainAss.CustomAttributes.Add(forwardAttribute);
                    mainAss.MainModule.ExportedTypes.Add(new ExportedType(studioType.Namespace, studioType.Name, studioType.Module, studioAss.Name));
                }
            }

            // Need to either load from memory or place the file in somewhere like bepinex\core and let it get loaded later to avoid loading in place of Assembly-csharp
            byte[] outputAss = null;
            using (var ms = new MemoryStream())
            {
                studioAss.Write(ms, new WriterParameters() { WriteSymbols = false });
                outputAss = ms.ToArray();
            }
            //Assembly.Load(outputAss);

            // If assembly dumping is turned on, dump the generated studio assembly
            var s = (ConfigFile)typeof(ConfigFile).GetProperty("CoreConfig", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null, null);
            if (s != null && s.TryGetEntry<bool>("Preloader", "DumpAssemblies", out var entry) && entry.Value)
            {
                var studioDumpPath = Path.Combine(Paths.BepInExRootPath, "DumpedAssemblies", newStudioAssemblyName + ".dll");
                File.WriteAllBytes(studioDumpPath, outputAss);
            }

            Logger.LogDebug($"Finished patching main game assembly with missing classes in {sw.ElapsedMilliseconds}ms");
        }

        static void ResolveMethod(MethodDefinition mdef, AssemblyDefinition targetAssembly, AssemblyDefinition otherAssembly)
        {
            foreach (var methodParameter in mdef.Parameters)
                methodParameter.ParameterType = ResolveReference(methodParameter.ParameterType, targetAssembly, otherAssembly);

            mdef.ReturnType = ResolveReference(mdef.ReturnType, targetAssembly, otherAssembly);

            if (mdef.Body != null)
            {
                //mdef.Body.Instructions.Clear();
                //mdef.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                //mdef.Body.Variables.Clear();
                //mdef.Body.ExceptionHandlers.Clear();

                //foreach (var gp in studioMethod.GenericParameters)
                //{
                //    gp.Constraints.Clear();
                //}

                foreach (var i in mdef.Body.Instructions)
                {
                    if (i.Operand is TypeReference r)
                        i.Operand = ResolveReference(r, targetAssembly, otherAssembly);
                    if (i.Operand is MethodReference m)
                    {
                        var declaring = ResolveReference(m.DeclaringType, targetAssembly, otherAssembly);
                        if (declaring != m.DeclaringType)
                        {
                            var otherMet = declaring.Resolve().Methods.FirstOrDefault(x => x.FullName == m.FullName);
                            if (otherMet != null)
                            {
                                i.Operand = targetAssembly.MainModule.ImportReference(otherMet);
                                continue;
                            }
                        }
                    }
                }
            }
        }

        // Thanks to Horse for this code
        private static TypeReference ResolveReference(TypeReference tr, AssemblyDefinition targetAssembly, AssemblyDefinition otherAssembly)
        {
            var result = ResolveReference2(tr, targetAssembly, otherAssembly);

            if (result == tr)
            {
            }
            return result;
        }
        private static TypeReference ResolveReference2(TypeReference tr, AssemblyDefinition targetAssembly, AssemblyDefinition otherAssembly)
        {
            // Lookup the type by its full name in the other assembly
            TypeReference TryResolve(TypeReference r)
            {
                var td = otherAssembly.MainModule.GetType(r.FullName);
                if (td == null)
                {
                    return null;
                }

                return targetAssembly.MainModule.ImportReference(td);
            }

            var result = (TypeReference)null;

            // Check if it's byref, array or generic and import appropriately
            // Might need to add pointer types if needed
            switch (tr)
            {
                case ArrayType ar:
                    result = TryResolve(ar.ElementType);
                    if (result != null)
                        result = new ArrayType(result);
                    break;

                case ByReferenceType rr:
                    result = TryResolve(rr.ElementType);
                    if (result != null)
                        result = new ByReferenceType(result);
                    break;

                case GenericInstanceType git:

                    result = TryResolve(git.ElementType);
                    if (result != null)
                    {
                        var git2 = new GenericInstanceType(result);
                        foreach (var gitGenericArgument in git.GenericArguments)
                            git2.GenericArguments.Add(ResolveReference(gitGenericArgument, targetAssembly, otherAssembly));
                        result = git2;
                    }
                    break;
            }

            if (result != null) return result;
            result = TryResolve(tr);
            if (result != null) return result;
            return tr;
        }
    }
}