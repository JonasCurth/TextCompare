using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comparer.Utils.Extensions
{
    internal static class CompatibilityExtensions
    {
        // JScript splice function
        public static List<T> Splice<T>(this List<T> input, int start, int count,
            params T[] objects)
        {
            List<T> deletedRange = input.GetRange(start, count);
            input.RemoveRange(start, count);
            input.InsertRange(start, objects);

            return deletedRange;
        }

        // Java substring function
        public static string JavaSubstring(this string s, int begin, int end)
        {
            return s.Substring(begin, end - begin);
        }
    }
}
