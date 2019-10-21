using System.Reflection;
using System.Runtime.InteropServices;
using Common;

// Information in this file is shared across all projects

[assembly: AssemblyCompany("https://github.com/IllusionMods/IllusionFixes")]
[assembly: AssemblyCopyright("Copyright ©  2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif

[assembly: ComVisible(false)]

[assembly: AssemblyVersion(Metadata.PluginsVersion)]
[assembly: AssemblyFileVersion(Metadata.PluginsVersion)]
