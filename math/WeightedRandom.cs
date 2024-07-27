using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class WeightedRandom<T>
{
    struct Entry
    {
        public int Weight;
        public T Value;
    }

    List<Entry> Values = new List<Entry>();
    int Total;

    public void AddValue(int weight, in T value)
    {
        Values.Add(new Entry { Weight = weight, Value = value });
        Total = checked(Total + weight);
    }

    public ref readonly T Sample()
    {
        var target = Util.RandInt(0, Total);

        var accum = 0;
        var idx = 0;

        while (idx < Values.Count - 1)
        {
            accum += Values[idx].Weight;
            if (accum > target) return ref CollectionsMarshal.AsSpan(Values)[idx].Value;
            ++idx;
        }

        return ref CollectionsMarshal.AsSpan(Values)[idx].Value;
    }
}