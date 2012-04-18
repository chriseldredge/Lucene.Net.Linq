using System;
using System.Collections.Generic;

namespace Lucene.Net.Linq.Util
{
    public static class LinqExtensions
    {
        public static void Apply<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
            {
                action(item);
            }
        }
    }
}