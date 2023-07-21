using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Godot;

public static class AT
{
    public static void True(bool cond, bool crit = false)
    {
#if TOOLS
        Eq(cond, true, crit);
#endif
    }

    public static void LessThan<T>(T a, T b, bool crit = false) where T : IComparable<T>
    {
#if TOOLS
        if (a.CompareTo(b) >= 0) Failed($"Expected {a} < {b}", crit);
#endif
    }

    public static void GreaterThan<T>(T a, T b, bool crit = false) where T : IComparable<T>
    {
#if TOOLS
        if (a.CompareTo(b) <= 0) Failed($"Expected {a} > {b}", crit);
#endif
    }

    public static void Within<T>(T v, T min, T max, bool crit = false) where T : IComparable<T>
    {
#if TOOLS
        if (v.CompareTo(max) > 0) Failed($"Expected {v} <= {max}", crit);
        if (v.CompareTo(min) < 0) Failed($"Expected {v} >= {min}", crit);
#endif
    }

    public static T Null<T>(T val, bool crit = false)
    {
#if TOOLS
        if (val != null) Failed("Expected null", crit);
#endif
        return val;
    }

    public static T NotNull<T>(T val, bool crit = false)
    {
#if TOOLS
        if (val == null) Failed("Expected non-null", crit);
#endif
        return val;
    }

    public static void Eq<T>(T a, T b, bool crit = false) where T : IEquatable<T>
    {
#if TOOLS
        if (!a.Equals(b)) Failed($"Expected {a} = {b}", crit);
#endif
    }

    public static void NotEq<T>(T a, T b, bool crit = false) where T : IEquatable<T>
    {
#if TOOLS
        if (a.Equals(b)) Failed($"Expected {a} != {b}", crit);
#endif
    }

    public static void Contains<T>(IEnumerable<T> en, T v, bool crit = false)
    {
#if TOOLS
        if (!en.Contains(v)) Failed($"Expected [{String.Join(", ", en)}] to contain {v}", crit);
#endif
    }

    public static void Disjoint<T>(IEnumerable<T> a, IEnumerable<T> b, bool crit = false)
    {
#if TOOLS
        if (a.Intersect(b).Any()) Failed("Unexpected intersection", crit);
#endif
    }

    public static void DoesNotContain<T>(IEnumerable<T> en, T v, bool crit = false)
    {
#if TOOLS
        if (en.Contains(v)) Failed($"Expected [{String.Join(", ", en)}] to NOT contain {v}", crit);
#endif
    }

    public static void NoDuplicates<T>(IEnumerable<T> en, bool crit = false)
    {
#if TOOLS
        if (en.Distinct().Count() != en.Count()) Failed($"Expected {en.Distinct().Count()} = {en.Count()}", crit);
#endif
    }

    public static void OnMainThread(bool crit = false)
    {
#if TOOLS
        if (System.Threading.Thread.CurrentThread.ManagedThreadId != 1) Failed($"We are on thread with ID {System.Threading.Thread.CurrentThread.ManagedThreadId}, expected 1", crit);
#endif
    }

    // assert that the current thread has an exclusive lock on this object
    public static void OnOwningThread(object obj, bool crit = false)
    {
#if TOOLS
        if (!Monitor.IsEntered(obj)) Failed($"Current thread with ID {System.Threading.Thread.CurrentThread.ManagedThreadId}, does not own monitor on {obj}", crit);
#endif
    }

    class TimeLimitHolder
    {
        public DateTime DateTime = DateTime.Now;
    }

    private static ConditionalWeakTable<object, TimeLimitHolder> TimeLimitMap = new ConditionalWeakTable<object, TimeLimitHolder>();

    public static object TimeLimit(object obj, string text = "", float? limitSeconds = 5, bool crit = false)
    {
#if TOOLS
        if (!OS.IsDebugBuild() || limitSeconds == null || obj == null) return obj;

        var dt = TimeLimitMap.GetOrCreateValue(obj);

        var actualTime = (DateTime.Now - dt.DateTime).TotalSeconds;
        if (actualTime > limitSeconds)
        {
            Failed($"Time limit of {limitSeconds} exceeded, actually took {actualTime}\n{System.Environment.StackTrace}\n{text}", crit);
            dt.DateTime = DateTime.Now;
        }
#endif

        return obj;
    }

    public struct TimeLimiter
    {
        public ulong StartTime = Time.GetTicksUsec();

        public int[] StartingGCS = new int[]{
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
        };

        public TimeLimiter() { }

        public void Limit(float limitSeconds, string text = "", bool crit = false)
        {
            var limitSecondsUsec = (ulong)(limitSeconds * 1_000_000f);
            var elapsedTimeUsec = (Time.GetTicksUsec() - StartTime);

            if (elapsedTimeUsec > limitSecondsUsec)
            {
                if (GC.CollectionCount(0) != StartingGCS[0] || GC.CollectionCount(1) != StartingGCS[1] || GC.CollectionCount(2) != StartingGCS[2])
                {
                    //GD.PushWarning("Time limit was exceeded, but GC was detected during timing");
                }
                else
                {
                    if (limitSecondsUsec < 1_000)
                    {
                        Failed($"Time limit of {limitSecondsUsec}μs exceeded, actually took {elapsedTimeUsec}μs\n{System.Environment.StackTrace}\n{text}", crit);
                    }
                    else if (limitSeconds < 1_000_000)
                    {
                        Failed($"Time limit of {limitSecondsUsec / 1_000}ms exceeded, actually took {elapsedTimeUsec / 1_000}ms\n{System.Environment.StackTrace}\n{text}", crit);
                    }
                    else
                    {
                        Failed($"Time limit of {limitSeconds} exceeded, actually took {new TimeSpan((long)(elapsedTimeUsec * 10))}\n{System.Environment.StackTrace}\n{text}", crit);
                    }
                }
            }

            StartTime = Time.GetTicksUsec();
        }
    }

    public static TimeLimiter TimeLimit()
    {
        return new TimeLimiter();
    }

    public static void Failed(string msg, bool crit)
    {
        string pos = "???";

        foreach (var it in System.Environment.StackTrace.Split("\n"))
        {
            if (!it.Contains("AT.") && !it.Contains("System.Environment."))
            {
                pos = it;
                break;
            }
        }

        var match = Regex.Match(pos, @"(\w+\.\w+):line (\d+)");
        var posSliced = "???";
        if (match.Success)
        {
            posSliced = $"{match.Groups[1].Value}:{match.Groups[2].Value}";
        }

        if (crit)
            throw new System.Exception($"{msg} @ {posSliced}");
        else
            GD.PushError($"{msg} @ {posSliced}");
    }

    public static void ReferenceNotEqual<T>(T a, T b, bool crit = false)
    {
#if TOOLS
        if (System.Object.ReferenceEquals(a, b)) Failed($"Expected {a} !ref= {b}", crit);
#endif
    }
}