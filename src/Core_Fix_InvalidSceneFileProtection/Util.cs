using System;
using System.IO;
using System.Linq;

namespace IllusionFixes
{        
    internal class KMPSearch
    {
        private readonly byte[] pattern;
        private int[] lps; // Longest Proper Prefix which is also Suffix

        public KMPSearch(byte[] pattern)
        {
            this.pattern = pattern;
            this.lps = ComputeLPSArray();
        }

        public bool Search(byte[] text, int n)
        {
            int m = pattern.Length;

            int i = 0; // index for text[]
            int j = 0; // index for pattern[]

            while (i < n)
            {
                if (pattern[j] == text[i])
                {
                    j++;
                    i++;
                }

                if (j == m)
                {
                    return true; // Pattern found
                }
                else if (i < n && pattern[j] != text[i])
                {
                    if (j != 0)
                    {
                        j = lps[j - 1];
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            return false; // Pattern not found
        }

        private int[] ComputeLPSArray()
        {
            int m = pattern.Length;
            int[] lps = new int[m];
            int len = 0; // length of the previous longest prefix suffix

            lps[0] = 0;
            int i = 1;

            while (i < m)
            {
                if (pattern[i] == pattern[len])
                {
                    len++;
                    lps[i] = len;
                    i++;
                }
                else
                {
                    if (len != 0)
                    {
                        len = lps[len - 1];
                    }
                    else
                    {
                        lps[i] = 0;
                        i++;
                    }
                }
            }

            return lps;
        }
    }
}