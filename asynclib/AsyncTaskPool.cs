using System;
using System.Threading.Tasks;
using Godot;

public partial class AsyncTaskPool : Node
{
    class PoolTask
    {
        public Task<object> OtherThreadFunc;
        public Action<object> MainThreadFunc;
    }

    public static void Run(Node ctx, Action otherThreadFunc, Action mainThreadFunc)
    {
        Run(ctx, () =>
        {
            otherThreadFunc();
            return 0;
        }, it =>
        {
            mainThreadFunc();
        });
    }

    public static void Run<T>(Node ctx, Func<T> otherThreadFunc, Action<T> mainThreadFunc)
    {
        GetSingleton(ctx).PoolTasks += new PoolTask
        {
            OtherThreadFunc = Task.Run<object>(() => otherThreadFunc()),
            MainThreadFunc = (it) => mainThreadFunc((T)it),
        };
    }

    COWList<PoolTask> PoolTasks = new();

    private static AsyncTaskPool GetSingleton(Node ctx)
    {
        var existing = ctx.GetTree().Root.FindChildByType<AsyncTaskPool>();
        if (existing == null)
        {
            ctx.GetTree().Root.AddChild(new AsyncTaskPool());
            existing = ctx.GetTree().Root.FindChildByType<AsyncTaskPool>();
        }

        return existing;
    }

    public override void _Process(double delta)
    {
        foreach (var it in PoolTasks)
        {
            if (it.OtherThreadFunc.IsCompleted)
            {
                try
                {
                    if (it.OtherThreadFunc.IsCompletedSuccessfully)
                    {
                        it.MainThreadFunc(it.OtherThreadFunc.Result);
                    }
                    else
                    {
                        GD.PushWarning($"Async Task Failed: {it.OtherThreadFunc.Exception}");
                    }
                }
                catch (Exception ex)
                {
                    GD.PushError(ex);
                }

                PoolTasks -= it;
            }
        }
    }
}