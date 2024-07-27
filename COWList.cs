using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

[DataContract]
public class COWList<T> : IReadOnlyList<T>, IEnumerable<T>, IReadOnlyCollection<T>
{
    [DataMember(EmitDefaultValue = false)] private T[] Data = Array.Empty<T>();

    public T this[int index] => Data[index];

    public int Count => Data.Length;

    public bool IsReadOnly => true;

    public COWList(IEnumerable<T> col)
    {
        Data = col.ToArray();
    }

    public COWList(params T[] col)
    {
        Data = col.ToArray();
    }

    private COWList() { }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)Data).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Data.GetEnumerator();
    }

    public static COWList<T> operator +(COWList<T> self, T other)
    {
        return new COWList<T>
        {
            Data = self.Data.Append(other).ToArray()
        };
    }

    public static COWList<T> operator +(COWList<T> self, IEnumerable<T> other)
    {
        return new COWList<T>
        {
            Data = self.Data.Concat(other).ToArray()
        };
    }

    public static COWList<T> operator -(COWList<T> self, T other)
    {
        var tmp = self.Data.ToList();
        tmp.Remove(other);
        return new COWList<T>
        {
            Data = tmp.ToArray(),
        };
    }

    public static COWList<T> operator -(COWList<T> self, IEnumerable<T> other)
    {
        var tmp = self.Data.ToList();
        tmp.RemoveAll(it => other.Contains(it));
        return new COWList<T>
        {
            Data = tmp.ToArray(),
        };
    }

    public COWList<T> ReplaceAt(T obj, int idx)
    {
        var dataCopy = Data.ToArray();
        dataCopy[idx] = obj;
        return new COWList<T> { Data = dataCopy };
    }

    public static readonly COWList<T> Empty = new();

    public static COWList<T> OfDefaults(int count)
    {
        var ret = new List<T>();
        for (var i = 0; i < count; ++i) ret.Add(default);
        return new COWList<T> { Data = ret.ToArray() };
    }
}

public static class COWListUtil
{
    public static COWList<T> ToCOWList<T>(this IEnumerable<T> col)
    {
        return new COWList<T>(col);
    }
}