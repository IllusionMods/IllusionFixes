using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx.Preloader.Core.Patching;
using Common;

namespace Patch_ByteFiddler_IL2CPP
{
    [PatcherPluginInfo("ByteFiddler", "ByteFiddler", Constants.PluginsVersion)]
    public class ByteFiddlerPatcher : BasePatcher
    {
        public override void Initialize()
        {
            var sw = Stopwatch.StartNew();

            var moduleName = Config.Bind("Edit Bytes", "Target Module Name", "GameAssembly.dll", "Name of a module in current process that of which memory should be searched.").Value.Trim();
            var patternStr = Config.Bind("Edit Bytes", "Bytes To Find", "", "List of bytes in hex to find in the process memory. (e.g. F0 0F 69 69)").Value;
            var replaceStr = Config.Bind("Edit Bytes", "Bytes To Write", "", "List of bytes in hex to write at the found address. (e.g. B0 0B 13 37)").Value;

            if (moduleName.Length == 0)
            {
                Log.LogInfo("Empty module name, doing nothing.");
                return;
            }

            ProcessModule module;
            try
            {
                module = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().First(x => x.ModuleName == moduleName);
            }
            catch (Exception e)
            {
                Log.LogError($"Failed to find module {moduleName} - {e}");
                return;
            }

            byte[] pattern;
            byte[] patch;

            try
            {
                pattern = patternStr.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => Convert.ToByte(x, 16)).ToArray();
                patch = replaceStr.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => Convert.ToByte(x, 16)).ToArray();
            }
            catch (Exception e)
            {
                Log.LogError("Failed to parse settings: " + e);
                return;
            }

            if (pattern.Length == 0 || patch.Length == 0)
            {
                Log.LogInfo("Empty pattern or patch, doing nothing.");
                return;
            }

            unsafe
            {
                var baseAddress = module.BaseAddress;
                var memorySize = module.ModuleMemorySize;

                using var stream = new UnmanagedMemoryStream((byte*)baseAddress, memorySize, memorySize, FileAccess.ReadWrite);

                var position = FindPosition(stream, pattern);

                if (position < 0)
                {
                    Log.LogWarning("Could not find the byte pattern, check the settings!");
                    return;
                }

                Log.LogInfo($"Found byte pattern at 0x{baseAddress.ToInt64() + position:X}, replacing...");

                stream.Seek(position, SeekOrigin.Begin);

                var matchPtr = (IntPtr)stream.PositionPointer;
                if (!NativeMethods.VirtualProtect(matchPtr, (UIntPtr)patch.Length, NativeMethods.PAGE_EXECUTE_READWRITE, out var oldProtect))
                {
                    Log.LogError($"Failed to change memory protection, aborting. Error code: {Marshal.GetLastWin32Error()}");
                    return;
                }

                stream.Write(patch, 0, patch.Length);

                NativeMethods.VirtualProtect(matchPtr, (UIntPtr)patch.Length, oldProtect, out _);

                Log.LogInfo($"Bytes overwritten successfully in {sw.ElapsedMilliseconds}ms!");
            }
        }

        private static long FindPosition(Stream stream, byte[] pattern)
        {
            long foundPosition = -1;
            int i = 0;
            int b;

            while ((b = stream.ReadByte()) > -1)
            {
                if (pattern[i++] != b)
                {
                    stream.Position -= i - 1;
                    i = 0;
                    continue;
                }

                if (i == pattern.Length)
                {
                    foundPosition = stream.Position - i;
                    break;
                }
            }

            return foundPosition;
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

            public static uint PAGE_EXECUTE_READWRITE = 0x40;
        }
    }
}
