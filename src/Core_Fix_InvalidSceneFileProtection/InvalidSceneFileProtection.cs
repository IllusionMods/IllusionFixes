using BepInEx.Logging;
using HarmonyLib;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Threading;

namespace IllusionFixes
{
    public partial class InvalidSceneFileProtection
    {
        public const string PluginName = "Invalid Scene Protection";

        private static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            var hi = Harmony.CreateAndPatchAll(typeof(InvalidSceneFileProtection));
            var tpl = new HarmonyMethod(typeof(InvalidSceneFileProtection), nameof(AddExceptionHandler));
            hi.Patch(AccessTools.Method(typeof(SceneInfo), nameof(SceneInfo.Load), new[] { typeof(string), typeof(Version).MakeByRefType() }), null, null, tpl);
            hi.Patch(AccessTools.Method(typeof(SceneInfo), nameof(SceneInfo.Import), new[] { typeof(string) }), null, null, tpl);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneLoadScene), "OnClickLoad")]
        private static bool OnClickLoadPrefix(List<string> ___listPath, int ___select)
        {
            var path = ___listPath[___select];
            return IsFileValid(path);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneLoadScene), "OnClickImport")]
        private static bool OnClickImportPrefix(List<string> ___listPath, int ___select)
        {
            var path = ___listPath[___select];
            return IsFileValid(path);
        }

        class Chunk
        {
            public static int Size = 1 << 20;
            public byte[] bytes = new byte[Size];
            public bool last;
            public int readed;
        }

        class SearchStatus
        {
            public volatile bool found;
        }

        class PoolAllocator<T> where T : new()
        {
            public T Acquire()
            {
                lock(m_Queue)
                {
                    if (m_Queue.Count > 0)
                        return m_Queue.Dequeue();
                }

                return new T();
            }

            public void Release( T value )
            {
                lock (m_Queue)
                {
                    m_Queue.Enqueue(value);
                }   
            }

            private Queue<T> m_Queue = new Queue<T>();
        }

        private static bool IsFileValid(string path)
        {
            if (!File.Exists(path)) return false;

            try
            {
                using (var fs = File.OpenRead(path))
                {
                    PngFile.SkipPng(fs);

                    var searchers = ValidStudioTokens.Select(token => new BoyerMoore(token)).ToArray();
                    int maxTokenSize = ValidStudioTokens.Max(token => token.Length);
                    
                    PoolAllocator<Chunk> allocator = new PoolAllocator<Chunk>();                    
                    ManualResetEvent waitEvent = new ManualResetEvent(false);
                    SearchStatus status = new SearchStatus();

                    void _Search( object _chunk )
                    {
                        Chunk chunk = (Chunk)_chunk;
                        bool found = false;

                        for (int i = 0; i < searchers.Length; ++i)
                            if (searchers[i].Contains(chunk.bytes, chunk.readed))
                            {
                                status.found = found = true;
                                break;
                            }

                        if (found || chunk.last)
                            waitEvent.Set();

                        allocator.Release(chunk);
                    }

                    while ( !status.found )
                    {
                        var chunk = allocator.Acquire();
                        chunk.readed = fs.Read(chunk.bytes, 0, chunk.bytes.Length);
                        chunk.last = fs.Position == fs.Length;

                        System.Threading.ThreadPool.QueueUserWorkItem( _Search, chunk );

                        if (chunk.last)
                            break;

                        //Slide a little because there may be data on the border.
                        fs.Position -= maxTokenSize - 1;
                    }

                    //Waiting for search to finish
                    waitEvent.WaitOne();

                    if (status.found)
                        return true;
                }

                LogInvalid();
                return false;
            }
            catch (Exception e)
            {
                // If the check crashes then don't prevent loading the scene in case it's actually good and this code is not
                Logger.LogError(e);
                return true;
            }
        }

        /// <summary>
        /// Converts the using block into a try catch finally that eats the exception instead of letting it crash upwards
        /// </summary>
        private static IEnumerable<CodeInstruction> AddExceptionHandler(IEnumerable<CodeInstruction> inst, ILGenerator ilGenerator)
        {
            var instructions = inst.ToList();

            var finallyBlockIndex = instructions.FindLastIndex(c => c.blocks.Count == 1 && c.blocks[0].blockType == ExceptionBlockType.BeginFinallyBlock);

            var catchBlock = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InvalidSceneFileProtection), nameof(LogCrash)));
            catchBlock.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(SystemException)));
            instructions.Insert(finallyBlockIndex, catchBlock);
            var endLabel = ilGenerator.DefineLabel();
            instructions.Insert(finallyBlockIndex + 1, new CodeInstruction(OpCodes.Leave, endLabel));

            var loadFalse = new CodeInstruction(OpCodes.Ldc_I4_0);
            loadFalse.labels.Add(endLabel);
            instructions.Add(loadFalse);
            instructions.Add(new CodeInstruction(OpCodes.Ret));

            return instructions;
        }

        private static void LogCrash(Exception ex)
        {
            Logger.Log(BepInEx.Logging.LogLevel.Message | BepInEx.Logging.LogLevel.Warning, "Failed to load the file - This scene is from a different game or it is corrupted");
            Logger.LogDebug(ex);
        }

        private static void LogInvalid()
        {
            Logger.Log(BepInEx.Logging.LogLevel.Message | BepInEx.Logging.LogLevel.Warning, "Cannot load the file - This is not a studio scene, it's a scene from a different game, or it is corrupted");
        }
    }
}