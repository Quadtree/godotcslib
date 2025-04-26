using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

[DataContract]
public class COWList<T> : IReadOnlyList<T>, IEnumerable<T>, IReadOnlyCollection<T>
{
    public class SettingsType
    {
        public bool DoNotAllowDuplicates { get; init; } = false;
        public bool FailedRemovalIsError { get; init; } = false;
    }

    [DataMember(EmitDefaultValue = false)] private T[] Data = Array.Empty<T>();

    public T this[int index] => Data[index];

    public int Count => Data.Length;

    public bool IsReadOnly => true;

    [DataMember(EmitDefaultValue = false)] public SettingsType Settings { get; init; }

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
        if (self.Settings?.DoNotAllowDuplicates == true && self.Contains(other)) throw new ArgumentException("This element is already in the list and duplicates are not allowed");

        return new COWList<T>
        {
            Data = self.Data.Append(other).ToArray(),
            Settings = self.Settings,
        };
    }

    public static COWList<T> operator +(COWList<T> self, IEnumerable<T> other)
    {
        if (self.Settings?.DoNotAllowDuplicates == true && self.Data.Intersect(other).Any()) throw new ArgumentException("This element is already in the list and duplicates are not allowed");

        return new COWList<T>
        {
            Data = self.Data.Concat(other).ToArray(),
            Settings = self.Settings,
        };
    }

    public static COWList<T> operator -(COWList<T> self, T other)
    {
        var tmp = self.Data.ToList();
        if (tmp.Remove(other))
        {
            return new COWList<T>
            {
                Data = tmp.ToArray(),
                Settings = self.Settings,
            };
        }
        else
        {
            if (self.Settings?.FailedRemovalIsError == true) throw new Exception($"Tried to remove {other}, but it was not on the list");
            return self;
        }
    }

    public static COWList<T> operator -(COWList<T> self, IEnumerable<T> other)
    {
        if (self.Settings?.FailedRemovalIsError == true)
        {
            var foundInList = other.Count(it => self.Contains(it));

            if (foundInList != other.Count())
            {
                throw new Exception($"Tried to remove {other}, but one or more elements were not on the list");
            }
        }

        var tmp = self.Data.ToList();
        tmp.RemoveAll(it => other.Contains(it));
        return new COWList<T>
        {
            Data = tmp.ToArray(),
            Settings = self.Settings,
        };
    }

    public COWList<T> ReplaceAt(int idx, T obj)
    {
        var dataCopy = Data.ToArray();
        dataCopy[idx] = obj;
        return new COWList<T> { Data = dataCopy, Settings = Settings };
    }

    public COWList<T> WithSettings(SettingsType settings)
    {
        return new COWList<T> { Data = Data, Settings = settings };
    }

    public int IndexOf(T obj)
    {
        for (var i = 0; i < Data.Length; ++i) if (Data[i]?.Equals(obj) == true || (Data[i] == null && obj == null)) return i;

        return -1;
    }

    public COWList<T> Replace(T oldObject, T newObject)
    {
        var idx = IndexOf(oldObject);
        if (idx != -1) return ReplaceAt(idx, newObject);
        return this;
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