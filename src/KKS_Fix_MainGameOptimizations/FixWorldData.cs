using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using MessagePack;
using SaveData;


namespace IllusionFixes
{
    /// <summary>
    /// Fix to allow saving when the number of characters and their costumes is large and the total saved data exceeds 2 GB.
    /// </summary>
    public partial class MainGameOptimizations
    {
        static bool _inWorldSaveHooks = false;

        [HarmonyPatch(typeof(SaveData.WorldData), nameof(SaveData.WorldData.Save), typeof(string), typeof(string))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        static bool WorldDataSavePrefix( SaveData.WorldData __instance, string path, string fileName)
        {
            _inWorldSaveHooks = true;

            try
            {
                Illusion.Utils.File.OpenWrite(path + fileName, false, (Action<FileStream>)(f =>
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter((Stream)f))
                    {
                        var saveData = __instance;

                        // Data to be written by WorldData.GetBytes
                        byte[] buffer = MessagePackSerializer.Serialize<WorldData>(saveData);
                        binaryWriter.Write(buffer.Length);
                        binaryWriter.Write(buffer);
                        byte[] bytes1 = SaveData.Player.GetBytes(saveData.player);
                        binaryWriter.Write(bytes1.Length);
                        binaryWriter.Write(bytes1);
                        int count = saveData.heroineList.Count;
                        binaryWriter.Write(count);
                        for (int index = 0; index < count; ++index)
                        {
                            byte[] bytes2 = Heroine.GetBytes(saveData.heroineList[index]);
                            binaryWriter.Write(bytes2.Length);
                            binaryWriter.Write(bytes2);
                        }

                        // Data added by hooks behind normal save data. 
                        // Normal data is erased by WorldDataGetBytesPrefix and only the data to be added is stored.
                        var extensionData = SaveData.WorldData.GetBytes(saveData);
                        binaryWriter.Write(extensionData);                        
                    }
                        
                }));

                return false;
            }
            catch( System.Exception e )
            {
                UnityEngine.Debug.LogException(e);
                return true;
            }
            finally
            {
                _inWorldSaveHooks = false;
            }
        }

        /// <summary>
        /// Function assuming the following hooks to WorldData.GetBytes() in BepisPlugins
        /// https://github.com/IllusionMods/BepisPlugins/blob/abee07b2392e9d7db88a7f80e68230cf9a7268a8/src/KKS_ExtensibleSaveFormat/KKS.ExtendedSave.SaveData.Hooks.cs#L61
        /// </summary>
        [HarmonyPatch(typeof(SaveData.WorldData), nameof(SaveData.WorldData.GetBytes), typeof(SaveData.WorldData))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.VeryHigh)]
        private static bool WorldDataGetBytesPrefix(SaveData.WorldData saveData, ref byte[] __result)
        {
            if ( _inWorldSaveHooks )
            {
                __result = new byte[0];
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
