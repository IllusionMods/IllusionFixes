using System;

namespace IllusionFixes
{
    internal class BoyerMoore
    {
        private readonly byte[] _needle;
        private readonly int[] _badMatchTable;

        public BoyerMoore(byte[] needle)
        {
            _needle = needle ?? throw new ArgumentNullException(nameof(needle));
            _badMatchTable = BuildBadMatchTable(_needle);
        }

        public bool Contains(byte[] haystack, int haystackLen)
        {
            if (haystack == null)
                throw new ArgumentNullException(nameof(haystack));

            int needleLen = _needle.Length;

            if (needleLen == 0)
                return true;

            if (needleLen > haystackLen)
                return false;

            int skip = 0;

            while (haystackLen - skip >= needleLen)
            {
                int i = needleLen - 1;
                while (i >= 0 && _needle[i] == haystack[skip + i])
                {
                    i--;
                }
                if (i < 0)
                {
                    return true; // needle found in haystack
                }
                else
                {
                    skip += _badMatchTable[haystack[skip + i]];
                }
            }

            return false; // needle not found in haystack
        }

        private int[] BuildBadMatchTable(byte[] needle)
        {
            int len = needle.Length;
            int[] table = new int[256];

            for (int i = 0; i < table.Length; i++)
            {
                table[i] = len;
            }

            for (int i = 0; i < len - 1; i++)
            {
                table[needle[i]] = len - i - 1;
            }

            return table;
        }
    }
}