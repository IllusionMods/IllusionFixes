using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Mono.Cecil;

namespace IllusionFixes.Patchers
{
    public static partial class RestoreMissingFunctionsPatch
    {
        public static IEnumerable<string> TargetDLLs =>
            File.Exists(Path.Combine(Paths.GameRootPath, "abdata/chara/oo_hand.unity3d"))
                ? new string[0]
                : new string[1] { "Assembly-CSharp.dll" };

        public static void Patch(AssemblyDefinition ad)
        {
            var t = ad.MainModule.Types.First(x => x.Name == "ChaFileHair").NestedTypes.First(x => x.Name == "PartsInfo");
            if (t.Properties.Any(x => x.Name == "noShake")) return;
            Injector.PropertyInject(ad, t, "noShake", typeof(bool));

            var t2 = ad.MainModule.Types.First(x => x.Name == "ChaFileAccessory").NestedTypes.First(x => x.Name == "PartsInfo");
            Injector.PropertyInject(ad, t2, "noShake", typeof(bool));
        }

        public static void Finish()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }
}