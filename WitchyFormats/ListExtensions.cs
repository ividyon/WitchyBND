using System.Collections.Generic;

namespace WitchyFormats
{
    /// <summary>
    /// Taken from DSMS
    /// </summary>
    internal static class ListExtensions
    {
        public static T EchoAdd<T>(this List<T> list, T item)
        {
            list.Add(item);
            return item;
        }
    }
}
