using System;
using System.Threading.Tasks;
using Godot;

public partial class AmortizedFunction : Node
{
    public class Context
    {
        AmortizedFunction FuncHolder;

        internal Context(AmortizedFunction funcHolder)
        {
            this.FuncHolder = funcHolder;
        }

        public Task Yield()
        {
            return Delay(0);
        }

        public Task Delay(float seconds)
        {
            var next = new TaskCompletionSource();
            FuncHolder.NextAction = next.SetResult;
            FuncHolder.NextActionTimeLeft = seconds;
            FuncHolder.NextActionPredicate = null;
            return next.Task;
        }

        public Task When(Func<bool> predicate)
        {
            var next = new TaskCompletionSource();
            FuncHolder.NextAction = next.SetResult;
            FuncHolder.NextActionTimeLeft = 0;
            FuncHolder.NextActionPredicate = predicate;
            return next.Task;
        }
    }

    private Action NextAction;
    private float NextActionTimeLeft;
    private Func<bool> NextActionPredicate;
    private Task FuncTask;

    private Context LocalContext;

    public AmortizedFunction()
    {
        LocalContext = new Context(this);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!FuncTask.IsCompleted && NextAction != null)
        {
            NextActionTimeLeft -= (float)delta;
            if (NextActionTimeLeft <= 0 && (NextActionPredicate == null || NextActionPredicate()))
            {
                NextAction();
                NextAction = null;
            }
        }

        if (FuncTask.IsCompleted)
        {
            QueueFree();
        }
    }

    public static void Invoke(Node parent, Func<Context, Task> func)
    {
        var node = new AmortizedFunction();
        parent.AddChild(node);

        node.FuncTask = func(node.LocalContext);
    }
}