using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace IllusionFixes.Patchers
{
    /// <summary>
    /// Created by Horse.
    /// > Fixes random hangups in Illusion's new game (AI-Shoujo). Why? I dunno lol
    /// </summary>
    public static class MagicCarrotPatch
    {
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Sirenix.Utilities.dll", "Sirenix.Serialization.dll" };

        public static void Patch(AssemblyDefinition ad)
        {
            var assemblyUtilities = ad.MainModule.Types.FirstOrDefault(t => t.Name == "AssemblyUtilities");
            var cctor = assemblyUtilities?.Methods.FirstOrDefault(m => m.Name == ".cctor");

            if (cctor == null)
                return;
            assemblyUtilities.Methods.Remove(cctor);
        }
    }
}