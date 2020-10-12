using System;
using System.IO;
using System.Linq;

namespace IllusionFixes
{
    internal class Util
    {
        // https://stackoverflow.com/a/1472689
        public static long FindPosition(Stream stream, byte[] byteSequence)
        {
            if (byteSequence.Length > stream.Length)
                return -1;

            byte[] buffer = new byte[byteSequence.Length];

            while (stream.Read(buffer, 0, byteSequence.Length) == byteSequence.Length)
            {
                if (byteSequence.SequenceEqual(buffer))
                    return stream.Position - byteSequence.Length;
                else
                    stream.Position -= byteSequence.Length - PadLeftSequence(buffer, byteSequence);
            }

            return -1;
        }

        private static int PadLeftSequence(byte[] bytes, byte[] seqBytes)
        {
            int i = 1;
            while (i < bytes.Length)
            {
                int n = bytes.Length - i;
                byte[] aux1 = new byte[n];
                byte[] aux2 = new byte[n];
                Array.Copy(bytes, i, aux1, 0, n);
                Array.Copy(seqBytes, aux2, n);
                if (aux1.SequenceEqual(aux2))
                    return i;
                i++;
            }
            return i;
        }
    }
}