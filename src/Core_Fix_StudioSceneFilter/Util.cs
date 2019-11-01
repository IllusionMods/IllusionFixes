using System;
using System.IO;

namespace IllusionFixes
{
    internal class Util
    {
        public static bool TryReadUntilSequence(Stream stream, byte[] sequence)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (sequence.Length == 0)
                return false;

            while (true)
            {
                var matched = false;
                for (var i = 0; i < sequence.Length; i++)
                {
                    var value = stream.ReadByte();
                    if (value == -1) return false;
                    matched = value == sequence[i];
                    if (matched) continue;
                    stream.Position -= i;
                    break;
                }
                if (matched)
                    return true;
            }
        }
    }
}