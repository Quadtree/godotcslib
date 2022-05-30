using System;
using System.Collections.Generic;
using Godot;

public class ChangeMonitor<T>
{
    Func<T> Func;
    public ulong LastKey { get; private set; }
    T LastValue;
    Func<T, ulong> KeyTransformer;

    public interface IChangeHash
    {
        IEnumerable<uint> GetChangeHash();
    }

    public ChangeMonitor(Func<T> func, Func<T, string> keyTransformer)
    {
        this.KeyTransformer = it => keyTransformer(it).Hash();
        this.Func = func;
    }

    public ChangeMonitor(Func<T> func, Func<T, IEnumerable<uint>> keyTransformer)
    {
        this.KeyTransformer = it => HashEnumerable(keyTransformer(it));
        this.Func = func;
    }

    public ChangeMonitor(Func<T> func, Func<T, ulong> keyTransformer = null)
    {
        if (keyTransformer == null)
            this.KeyTransformer = ChangeMonitor<T>.DefaultKeyTransformer;
        else
            this.KeyTransformer = keyTransformer;
        this.Func = func;
    }

    public bool IsChanged
    {
        get
        {
            var newValue = Func();
            var newKey = KeyTransformer(newValue);
            if (newKey != LastKey)
            {
                LastKey = newKey;
                LastValue = newValue;
                return true;
            }
            return false;
        }
    }

    public T Get()
    {
        return LastValue;
    }

    static ulong HashEnumerable(IEnumerable<uint> en)
    {
        // This hashing scheme is designed to minimize string concatenations
        // Basically, it's based on the idea that:
        // a,b are both 2 or greater and are positive prime integers
        // x,y are nonzero positive integers
        // Assuming those are true, a*x != b*y

        ulong ret = 0;
        int primeIdx = 1;

        foreach (var hash in en)
        {
            ret += AddToHash(hash) * PrimeNumberSource.GetPrimeAtIdx(primeIdx++);
        }

        return ret;
    }

    static uint AddToHash(uint hash)
    {
        if (hash == uint.MaxValue)
        {
            hash = 1;
        }
        return hash + 1;
    }

    static ulong DefaultKeyTransformer(T val)
    {
        ulong ret = 0;
        int primeIdx = 1;

        if (val is IEnumerable<IChangeHash>)
        {
            foreach (var it in (IEnumerable<IChangeHash>)val)
            {
                foreach (var hash in it?.GetChangeHash() ?? Array.Empty<uint>())
                {
                    ret += AddToHash(hash) * PrimeNumberSource.GetPrimeAtIdx(primeIdx++);
                }
            }
        }
        else if (val is IChangeHash)
        {
            foreach (var hash in ((IChangeHash)val).GetChangeHash())
            {
                ret += AddToHash(hash) * PrimeNumberSource.GetPrimeAtIdx(primeIdx++);
            }
        }
        else if (val is IEnumerable<object>)
        {
            foreach (var it in (IEnumerable<object>)val)
            {
                ret += AddToHash((it?.ToString()?.Hash() ?? 982347)) * PrimeNumberSource.GetPrimeAtIdx(primeIdx++);
            }
        }
        else
        {
            ret = val?.ToString()?.Hash() ?? 0;
        }

        return ret;
    }
}