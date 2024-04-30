#region

using System;
using System.Collections.Generic;

#endregion

namespace ProcureEase.Classes;

public static class EnumerableExtensions
{
    public static (List<T>, List<T>) Partition<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        var positive = new List<T>();
        var negative = new List<T>();
        foreach (var element in source)
            if (predicate(element))
                positive.Add(element);
            else
                negative.Add(element);

        return (positive, negative);
    }
}