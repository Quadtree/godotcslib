using System;
using System.Collections.Generic;
using Godot;

public partial class AmortizedTimer : Node
{
    public float Interval
    {
        get
        {
            return IntervalUsec / 1_000_000;
        }
        set
        {
            IntervalUsec = (long)(value * 1_000_000.0);
        }
    }

    public long IntervalUsec;

    public bool RunOnPhysics;

    public long LastTriggered;

    public Func<float, IEnumerable<object>> TargetFunctionWithArg;

    public Func<IEnumerable<object>> TargetFunctionWithoutArg;

    private IEnumerator<object> ActiveRun;

    public override void _Ready()
    {
        base._Ready();

        LastTriggered = (long)Time.GetTicksUsec() - (IntervalUsec * (long)Util.RandInt(0, 1000) / 1000);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!RunOnPhysics) DoProcess();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (RunOnPhysics) DoProcess();
    }

    private void DoProcess()
    {
        if (ActiveRun != null)
        {
            if (ActiveRun.MoveNext())
                return;
            else
                ActiveRun = null;
        }

        if ((long)Time.GetTicksUsec() >= LastTriggered + IntervalUsec)
        {
            LastTriggered = (long)Time.GetTicksUsec();

            if (TargetFunctionWithArg != null)
                ActiveRun = TargetFunctionWithArg(Interval)?.GetEnumerator();
            else if (TargetFunctionWithoutArg != null)
                ActiveRun = TargetFunctionWithoutArg()?.GetEnumerator();
        }
    }

    public static void Create(Node parent, float intervalSec, Func<IEnumerable<object>> target, bool runOnPhysics = false)
    {
        parent.AddChild(new AmortizedTimer
        {
            Interval = intervalSec,
            RunOnPhysics = runOnPhysics,
            TargetFunctionWithoutArg = target
        });
    }

    public static void Create(Node parent, float intervalSec, Func<float, IEnumerable<object>> target, bool runOnPhysics = false)
    {
        parent.AddChild(new AmortizedTimer
        {
            Interval = intervalSec,
            RunOnPhysics = runOnPhysics,
            TargetFunctionWithArg = target
        });
    }
}