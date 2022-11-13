using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;

public static class AT
{
    public static void True(bool cond, bool crit = false)
    {
        if (!OS.IsDebugBuild()) return;
        Eq(cond, true, crit);
    }

    public static void LessThan<T>(T a, T b, bool crit = false) where T : IComparable<T>
    {
        if (!OS.IsDebugBuild()) return;
        if (a.CompareTo(b) >= 0) Failed($"Expected {a} < {b}", crit);
    }

    public static void Within<T>(T v, T min, T max, bool crit = false) where T : IComparable<T>
    {
        if (!OS.IsDebugBuild()) return;
        if (v.CompareTo(max) > 0) Failed($"Expected {v} <= {max}", crit);
        if (v.CompareTo(min) < 0) Failed($"Expected {v} >= {min}", crit);
    }

    public static T Null<T>(T val, bool crit = false)
    {
        if (!OS.IsDebugBuild()) return val;
        if (val != null) Failed("Expected null", crit);
        return val;
    }

    public static T NotNull<T>(T val, bool crit = false)
    {
        if (!OS.IsDebugBuild()) return val;
        if (val == null) Failed("Expected non-null", crit);
        return val;
    }

    public static void Eq<T>(T a, T b, bool crit = false) where T : IEquatable<T>
    {
        if (!OS.IsDebugBuild()) return;
        if (!a.Equals(b)) Failed($"Expected {a} = {b}", crit);
    }

    public static void NotEq<T>(T a, T b, bool crit = false) where T : IEquatable<T>
    {
        if (!OS.IsDebugBuild()) return;
        if (a.Equals(b)) Failed($"Expected {a} != {b}", crit);
    }

    public static void Contains<T>(IEnumerable<T> en, T v, bool crit = false)
    {
        if (!OS.IsDebugBuild()) return;
        if (!en.Contains(v)) Failed($"Expected [{String.Join(", ", en)}] to contain {v}", crit);
    }

    public static void Disjoint<T>(IEnumerable<T> a, IEnumerable<T> b, bool crit = false)
    {
        if (!OS.IsDebugBuild()) return;
        if (a.Intersect(b).Any()) Failed("Unexpected intersection", crit);
    }

    public static void DoesNotContain<T>(IEnumerable<T> en, T v, bool crit = false)
    {
        if (!OS.IsDebugBuild()) return;
        if (en.Contains(v)) Failed($"Expected [{String.Join(", ", en)}] to NOT contain {v}", crit);
    }

    public static void NoDuplicates<T>(IEnumerable<T> en, bool crit = false)
    {
        if (!OS.IsDebugBuild()) return;
        if (en.Distinct().Count() != en.Count()) Failed($"Expected {en.Distinct().Count()} = {en.Count()}", crit);
    }

    public static void OnMainThread(bool crit = false)
    {
        if (!OS.IsDebugBuild()) return;
        if (System.Threading.Thread.CurrentThread.ManagedThreadId != 1) Failed($"We are on thread with ID {System.Threading.Thread.CurrentThread.ManagedThreadId}, expected 1", crit);
    }

    class TimeLimitHolder
    {
        public DateTime DateTime = DateTime.Now;
    }

    private static ConditionalWeakTable<object, TimeLimitHolder> TimeLimitMap = new ConditionalWeakTable<object, TimeLimitHolder>();

    public static void TimeLimit<T>(T obj, string text = "", float? limitSeconds = 5, bool crit = false) where T : class
    {
        if (!OS.IsDebugBuild() || limitSeconds == null || obj == null) return;

        var dt = TimeLimitMap.GetOrCreateValue(obj);

        if ((DateTime.Now - dt.DateTime).TotalSeconds > limitSeconds)
        {
            Failed($"Time limit of {limitSeconds} exceeded\n{System.Environment.StackTrace}\n{text}", crit);
            dt.DateTime = DateTime.Now;
        }
    }

    public static void Failed(string msg, bool crit)
    {
        if (crit)
            throw new System.Exception(msg);
        else
            GD.PushError(msg);
    }

    public static void ReferenceNotEqual<T>(T a, T b, bool crit = false)
    {
        if (!OS.IsDebugBuild()) return;
        if (System.Object.ReferenceEquals(a, b)) Failed($"Expected {a} !ref= {b}", crit);
    }
}