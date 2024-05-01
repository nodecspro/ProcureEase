#region

#endregion

namespace ProcureEase.Classes;

public static class EnumerableExtensions
{
    public static (List<T>, List<T>) Partition<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var positive = new List<T>();
        var negative = new List<T>();

        // If the source collection is a type that allows for counting elements without enumeration
        if (source is ICollection<T> collection)
        {
            positive.Capacity = collection.Count;
            negative.Capacity = collection.Count;
        }

        foreach (var element in source)
            if (predicate(element))
                positive.Add(element);
            else
                negative.Add(element);

        return (positive, negative);
    }
}