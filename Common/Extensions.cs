using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    internal static class Extensions
    {
        public static T[] RemoveNulls<T>(this T[] array)
        {
            var list = array.ToList();

            for (int i = 0; i < list.Count;)
                if (list[i] == null)
                    list.RemoveAt(i);
                else
                    i++;

            return list.ToArray();
        }
    }
}
