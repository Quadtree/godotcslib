using System;
using System.Runtime.Serialization;

public struct SimpleTimer<T, R>
{
    public float Charge { get; set; }

    public float Period { get; init; }

    public Action<float, T, R> Target2 { get; init; }
    public Action<float, T> Target1 { get; init; }
    public Action<float> Target0 { get; init; }

    public bool IsValid => Target0 != null || Target1 != null || Target2 != null;

    public void Update(float delta, T v1, R v2)
    {
#if TOOLS
        if (Target0 == null && Target1 == null && Target2 == null) throw new Exception("All targets are null!");
#endif

        Charge += delta;
        if (Charge >= Period)
        {
            Charge -= Period;
#if TOOLS
            if (Target2 == null && Target1 != null) throw new Exception("This timer is supposed to only get 1 arg");
            if (Target2 == null && Target0 != null) throw new Exception("This timer is supposed to only get 0 args");
#endif
            Target2(Period, v1, v2);
        }
    }

    public void Update(float delta, T v1)
    {
        Charge += delta;
        if (Charge >= Period)
        {
            Charge -= Period;
            Target1(Period, v1);
        }
    }

    public void Update(float delta)
    {
        Charge += delta;
        if (Charge >= Period)
        {
            Charge -= Period;
            Target0(Period);
        }
    }
}

public static class SimpleTimerFactory
{
    public static SimpleTimer<T, R> Create<T, R>(float period, Action<float, T, R> target2)
    {
        AT.NotNull(target2, true);

        return new SimpleTimer<T, R>
        {
            Charge = Util.RandF(0, period),
            Period = period,
            Target2 = target2,
        };
    }

    public static SimpleTimer<T, object> Create<T>(float period, Action<float, T> target1)
    {
        return new SimpleTimer<T, object>
        {
            Charge = Util.RandF(0, period),
            Period = period,
            Target1 = target1,
        };
    }

    public static SimpleTimer<object, object> Create(float period, Action<float> target0)
    {
        return new SimpleTimer<object, object>
        {
            Charge = Util.RandF(0, period),
            Period = period,
            Target0 = target0,
        };
    }
}