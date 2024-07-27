using System;
using System.Collections.Generic;
using Godot;

public partial class RefChangeMonitor<T>
{
    ReadOnlyFunc Func;

    public ulong LastKey { get; private set; }
    KeyFunc2 KeyTransformer;

    public delegate ref readonly T ReadOnlyFunc();
    public delegate IEnumerable<uint> KeyFunc2(in T val);

    public interface IChangeHash
    {
        IEnumerable<uint> GetChangeHash();
    }

    public RefChangeMonitor(ReadOnlyFunc func, KeyFunc2 keyTransformer)
    {
        this.Func = func;
        this.KeyTransformer = keyTransformer;
    }

    public bool IsChanged
    {
        get
        {
            var newValue = Func();
            var newKey = HashEnumerable(KeyTransformer(newValue));
            if (newKey != LastKey)
            {
                LastKey = newKey;
                return true;
            }
            return false;
        }
    }

    public ref readonly T Get()
    {
        return ref Func();
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