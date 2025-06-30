using System;
using System.Collections.Generic;

public class ZList
{
    public static List<T> GetMostFrequentBy<T, TKey>(List<T> list, Func<T, TKey> keySelector)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list), "Input list cannot be null.");
        if (list.Count == 0)
        {
            return new List<T>();
        }

        Dictionary<TKey, int> counts = new Dictionary<TKey, int>();
        Dictionary<TKey, T> firstItems = new Dictionary<TKey, T>();

        foreach (var item in list)
        {
            TKey key = keySelector(item);

            if (counts.ContainsKey(key))
                counts[key]++;
            else
            {
                counts[key] = 1;
                firstItems[key] = item;
            }
        }

        int maxCount = 0;
        foreach (var val in counts.Values)
        {
            if (val > maxCount)
                maxCount = val;
        }

        List<T> result = new List<T>();
        foreach (KeyValuePair<TKey, int> pair in counts)
        {
            if (pair.Value == maxCount)
                result.Add(firstItems[pair.Key]);
        }
        return result;
    }
}
