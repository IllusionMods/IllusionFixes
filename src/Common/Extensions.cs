using System.Linq;

namespace Common
{
    internal static class Extensions
    {
        public static T[] RemoveNulls<T>(this T[] array)
        {
            var list = array.ToList();
            list.RemoveAll(x => x == null);
            return list.ToArray();
        }
    }
}
