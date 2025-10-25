using System;
using System.Collections.Generic;
using System.Linq;

public class ChangeMonitor2<T, R>
{
    private readonly Func<T> Func;
    public R[] LastKey { get; private set; } = [];
    public T LastValue { get; private set; }
    private readonly Func<T, IEnumerable<R>> KeyTransformer;

    public ChangeMonitor2(Func<T> func, Func<T, IEnumerable<R>> keyTransformer)
    {
        KeyTransformer = keyTransformer;
        Func = func;
    }

    public bool FetchLatest()
    {
        var newValue = Func();

        var possibleNewKey = KeyTransformer(newValue).ToArray();

        if (!Enumerable.SequenceEqual(LastKey, possibleNewKey))
        {
            LastKey = possibleNewKey;
            LastValue = newValue;
            return true;
        }

        return false;
    }
}