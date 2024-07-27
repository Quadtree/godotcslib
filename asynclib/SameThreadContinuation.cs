using System;
using System.Threading.Tasks;
using Godot;

#pragma warning disable

// by default, C# ContinueWith will schedule the operation on any thread, which is generally very risky
// in Godot. This class simply arrange for the next operation to be scheduled on the main thread
public static class SameThreadContinuation
{
    public static void SafeContinueWith(this Task task, Action callback)
    {
        _SafeContinueWith(task, callback);
    }

    private static async Task _SafeContinueWith(Task task, Action callback)
    {
        await task;
        if (callback != null) callback();
    }

    public static void SafeContinueWith<T>(this Task<T> task, Action<T> callback)
    {
        _SafeContinueWith(task, callback);
    }

    private static async Task _SafeContinueWith<T>(Task<T> task, Action<T> callback)
    {
        T data = await task;
        if (callback != null) callback(data);
    }

    public static void Then<T>(this Task<T> task, Action<T> callback, Action<Exception> errorHandler = null)
    {
        _Then(task, callback, errorHandler);
    }

    private static async Task _Then<T>(this Task<T> task, Action<T> callback, Action<Exception> errorHandler)
    {
        try
        {
            T data = await task;
            if (callback != null) callback(data);
        }
        catch (Exception err)
        {
            if (errorHandler != null)
                errorHandler(err);
            else
                GD.PushError(err);
        }
    }

    public static void Then(this Task task, Action callback, Action<Exception> errorHandler = null)
    {
        _Then(task, callback, errorHandler);
    }

    private static async Task _Then(this Task task, Action callback, Action<Exception> errorHandler)
    {
        try
        {
            await task;
            if (callback != null) callback();
        }
        catch (Exception err)
        {
            if (errorHandler != null)
                errorHandler(err);
            else
                GD.PushError(err.ToString());
        }
    }
}