using System.Collections.Generic;
using System.Linq;

public static class IListExtensions {
    /// <summary>
    /// Knuth (Fisher-Yates) Shuffle
    /// Shuffles the element order (in place) of the specified IList derived type.
    /// </summary>
    public static T ShuffleInPlace<T, U>(this T list) where T : IList<U> {
        var count = list.Count;
        for (int i = 0; i < count; ++i) {
            int r = InterfaceManager.rnd.Value.Next(i, count);
            U tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
        return list;
    }

    /// <summary>
    /// Knuth (Fisher-Yates) Shuffle
    /// Generates a shuffled version of the specified IList derived type.
    /// </summary>
    public static T Shuffle<T, U>(this T list) where T : IList<U>, new() {
        // In the future, when c# supports it, just do "T shuf = new T(list);"
        // and delete the concat usage. Also, remove "using System.Linq;" up top
        T shuf = new T();
        shuf = (T)(IList<U>)shuf.Concat(list).ToList();
        return shuf.ShuffleInPlace<T, U>();
    }

    /// <summary>
    /// Knuth (Fisher-Yates) Shuffle
    /// Generates a shuffled version of the specified IList.
    /// </summary>
    public static List<T> Shuffle<T>(this IList<T> list) {
        var shuf = new List<T>(list);
        return shuf.ShuffleInPlace<List<T>, T>();
    }

    // This exists solely because the C# type system is not smart enough
    // to infer types...
    public static IList<T> ShuffleInPlace<T>(this IList<T> list) {
        return list.ShuffleInPlace<IList<T>, T>();
    }

    // This exists solely because the C# type system is not smart enough
    // to infer types...
    public static List<T> ShuffleInPlace<T>(this List<T> list) {
        return list.ShuffleInPlace<List<T>, T>();
    }

    // This exists solely because the C# type system is not smart enough
    // to infer types...
    public static List<T> Shuffle<T>(this List<T> list) {
        return list.Shuffle<List<T>, T>();
    }
}

