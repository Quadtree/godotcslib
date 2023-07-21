using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class ResourceLoadMonitor : Node
{
    private static ConditionalWeakTable<SceneTree, ResourceLoadMonitor> Monitors = new ConditionalWeakTable<SceneTree, ResourceLoadMonitor>();

    class LoadingRequest
    {
        public Node ValidityCheckNode;
        public Action<Resource> Target;
        public Action<Exception> ErrorHandler;
    }

    class MonitoredItem
    {
        public List<LoadingRequest> Requests = new List<LoadingRequest>();
        public ulong LoadStartTimeUsec = Time.GetTicksUsec();
    }

    Dictionary<string, MonitoredItem> Monitored = new Dictionary<string, MonitoredItem>();

    class CacheEntry
    {
        public ulong LastUsedUSecTimestamp;
        public Resource Item;
    }

    Dictionary<string, CacheEntry> LoadedItems = new Dictionary<string, CacheEntry>();

    private Thread InstantiationThread;

    private volatile bool InstantiationThreadKeepRunning;

    // the current number of microseconds this process is allowed to run for
    // the default is 1ms. each time we hit the time limit, it's increased by 0.5ms
    private ulong TimeLimit;

    // whether instantiations should take place on a seperate thread. the docs indicate that this is possible, but unfortunately
    // testing indicates that it causes crashes and freezes sometimes
    [Export]
    public bool InstantiateOnSeperateThread = false;

    // If we detect that we're using more than this amount of VRAM, start evicting things from the cache
    // Since Godot doesn't seem to handle evictions very well, this is probably a bad idea, but otherwise we
    // may run out of VRAM
    // 0 means no limit
    [Export]
    public int VideoRAMLimitMiB = 0;

    [Export]
    public int CacheTimeSeconds = 30;

    class InstantiationThreadRequest
    {
        public volatile PackedScene Input;
        public volatile Node ValidityCheckNode;
        public volatile Action<object> MainThreadCallback;
        public volatile Action<Exception> MainThreadErrorHandler;
        public volatile object Output;
        public volatile Exception Err;
    }

    private ConcurrentQueue<InstantiationThreadRequest> InstantationThreadInputQueue = new ConcurrentQueue<InstantiationThreadRequest>();
    private ConcurrentQueue<InstantiationThreadRequest> InstantationThreadOutputQueue = new ConcurrentQueue<InstantiationThreadRequest>();

    public override void _EnterTree()
    {
        base._EnterTree();

        AT.Null(InstantiationThread);

        InstantiationThreadKeepRunning = true;

        if (InstantiateOnSeperateThread)
        {
            InstantiationThread = new Thread(ThreadEntryPoint);
            InstantiationThread.Start();
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        InstantiationThreadKeepRunning = false;
    }

    public override void _Ready()
    {
        base._Ready();

        ProcessMode = ProcessModeEnum.Always;

        Util.StartStaggeredPeriodicTimer(this, 5, ConsiderEviction);
    }

    public override void _Process(double delta)
    {
        var startTime = Time.GetTicksUsec();

        var toRemove = new List<string>();

        try
        {
            // the callbacks we call here could add in more monitored things, so we defensively copy this
            foreach (var kv in Monitored.ToArray())
            {
                if (Time.GetTicksUsec() - startTime > TimeLimit)
                {
                    TimeLimit += 500;
                    return;
                }

                var status = ResourceLoader.LoadThreadedGetStatus(kv.Key);

                if (status != ResourceLoader.ThreadLoadStatus.InProgress && (status != ResourceLoader.ThreadLoadStatus.InvalidResource || Time.GetTicksUsec() - kv.Value.LoadStartTimeUsec > 5_000_000))
                {
                    var loadedResource = ResourceLoader.LoadThreadedGet(kv.Key);
                    foreach (var target in kv.Value.Requests)
                    {
                        if (target.ValidityCheckNode.IsInstanceValid() && target.ValidityCheckNode.IsInsideTree())
                        {
                            AT.OnMainThread();
                            if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                            {
                                GD.Print($"Successfully loaded {kv.Key} VRAM={(long)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1024L / 1024L}MiB");

                                LoadedItems[kv.Key] = new CacheEntry
                                {
                                    Item = loadedResource,
                                    LastUsedUSecTimestamp = Time.GetTicksUsec(),
                                };

                                target.Target(loadedResource);
                            }
                            else
                            {
                                GD.Print($"Failed to load {kv.Key} due to status {status}");
                                target.ErrorHandler(new Exception($"Unexpected status {status} while loading {kv.Key}"));
                            }
                        }
                        else
                        {
                            GD.Print("ResourceLoadMonitor: Implicitly canceled loading due to node validity check failing");
                        }
                    }

                    AT.GreaterThan(kv.Value.Requests.Count, 0);

                    toRemove.Add(kv.Key);
                }
            }
        }
        finally
        {
            foreach (var it in toRemove) Monitored.Remove(it);
        }

        if (!InstantiateOnSeperateThread)
        {
            while (PumpInstantationQueue())
            {
                if (Time.GetTicksUsec() - startTime > TimeLimit)
                {
                    TimeLimit += 500;
                    return;
                }
            }
        }

        InstantiationThreadRequest req;

        while (InstantationThreadOutputQueue.TryDequeue(out req))
        {
            if (Time.GetTicksUsec() - startTime > TimeLimit)
            {
                TimeLimit += 500;
                return;
            }

            if (req.ValidityCheckNode.IsInstanceValid() && req.ValidityCheckNode.IsInsideTree())
            {
                AT.OnMainThread();
                if (req.Err != null)
                    req.MainThreadErrorHandler(req.Err);
                else
                    req.MainThreadCallback(req.Output);
            }
            else
            {
                GD.Print("ResourceLoadMonitor: Implicitly canceled instantation due to node validity check failing");
            }
        }

        TimeLimit = 1_000;
    }

    private void ConsiderEviction()
    {
        foreach (var it in LoadedItems.Where(it => ((long)Time.GetTicksUsec() - (long)it.Value.LastUsedUSecTimestamp) > CacheTimeSeconds * 1_000_000L).ToArray())
        {
            GD.Print($"ResourceLoadMonitor: Evicting {it.Key} as it has not been used in {((long)Time.GetTicksUsec() - (long)it.Value.LastUsedUSecTimestamp) / 1_000_000}s");
            it.Value.Item = null;
            LoadedItems.Remove(it.Key);
        }

        if (VideoRAMLimitMiB != 0)
        {
            while (VideoMemoryMiBUsed > VideoRAMLimitMiB && LoadedItems.Count > 0)
            {
                var toEvict = LoadedItems.MinBy(it => it.Value.LastUsedUSecTimestamp).Key;
                GD.Print($"ResourceLoadMonitor: Evicting {toEvict} due to excessive VRAM use");
                LoadedItems.Remove(toEvict);
            }
        }
    }

    private void DoStartLoading<T>(string resourcePath, Action<T> target, Node validityCheckNode, Action<Exception> errorHandler) where T : Resource
    {
        AT.OnMainThread();

        var status = ResourceLoader.LoadThreadedGetStatus(resourcePath);
        if (status == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            target((T)ResourceLoader.LoadThreadedGet(resourcePath));
            return;
        }
        else if (status != ResourceLoader.ThreadLoadStatus.InvalidResource && status != ResourceLoader.ThreadLoadStatus.InProgress)
        {
            throw new Exception($"Resource {resourcePath} is in unexpected status {status}");
        }

        if (LoadedItems.ContainsKey(resourcePath))
        {
            LoadedItems[resourcePath].LastUsedUSecTimestamp = Time.GetTicksUsec();
            target((T)LoadedItems[resourcePath].Item);
            return;
        }

        GD.Print($"ResourceLoader.LoadThreadedRequest({resourcePath})");
        ResourceLoader.LoadThreadedRequest(resourcePath);

        if (!Monitored.ContainsKey(resourcePath))
        {
            Monitored.Add(resourcePath, new MonitoredItem());
        }

        Monitored[resourcePath].Requests.Add(new LoadingRequest
        {
            ValidityCheckNode = validityCheckNode,
            Target = (trg) => target((T)trg),
            ErrorHandler = errorHandler,
        });
    }

    private int VideoMemoryMiBUsed = (int)((long)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1024L / 1024L);

    private void ThreadEntryPoint()
    {
        while (InstantiationThreadKeepRunning)
        {
            PumpInstantationQueue();

            Thread.Sleep(1);
        }

        InstantiationThread = null;
    }

    private bool PumpInstantationQueue()
    {
        InstantiationThreadRequest req;

        if (InstantationThreadInputQueue.TryDequeue(out req))
        {
            GD.Print($"Instantiating {req.Input} on thread '{Thread.CurrentThread.Name}/{System.Threading.Thread.CurrentThread.ManagedThreadId}'");
            try
            {
                req.Output = req.Input.Instantiate();
            }
            catch (Exception err)
            {
                req.Err = err;
            }
            GD.Print($"Finished instantiating {req.Output} on thread '{Thread.CurrentThread.Name}/{System.Threading.Thread.CurrentThread.ManagedThreadId}'");
            InstantationThreadOutputQueue.Enqueue(req);
            return true;
        }

        return false;
    }

    private static ResourceLoadMonitor EnsureExistingMonitor(Node validityCheckNode)
    {
        var tree = validityCheckNode.GetTree();

        ResourceLoadMonitor monitor;
        if (!Monitors.TryGetValue(tree, out monitor))
        {
            GD.Print($"Creating new ResourceLoadMonitor for tree {tree}");

            monitor = new ResourceLoadMonitor();
            Monitors.Add(tree, monitor);

            // this is needed because through a chain of calls we might still be setting up when this is called
            Action monitorAdder = null;
            monitorAdder = () =>
            {
                if (tree.Root.FindChildByType<ResourceLoadMonitor>() == null)
                {
                    tree.Root.AddChild(monitor);
                    GD.Print("ResourceLoadMonitor added to root");
                }
                else
                    GD.PushWarning("ResourceLoadMonitor NOT added to root");

                tree.ProcessFrame -= monitorAdder;
            };

            tree.ProcessFrame += monitorAdder;
        }

        return monitor;
    }

    public static void StartLoading<T>(string resourcePath, Node validityCheckNode, Action<T> target, Action<Exception> errorHandler) where T : Resource
    {
        AT.NotNull(resourcePath);
        AT.NotNull(target);
        AT.NotNull(validityCheckNode);

        try
        {
            EnsureExistingMonitor(validityCheckNode).DoStartLoading<T>(resourcePath, target, validityCheckNode, errorHandler);
        }
        catch (Exception err)
        {
            errorHandler(err);
        }
    }

    public static Task<T> StartLoadingAsync<T>(string resourcePath, Node validityCheckNode) where T : Resource
    {
        var src = new TaskCompletionSource<T>();
        StartLoading<T>(resourcePath, validityCheckNode, (res) => src.SetResult(res), (err) => src.SetException(err));
        return src.Task;
    }

    public static async Task<T> ThreadedInstantiateAsync<T>(string resourcePath, Node validityCheckNode) where T : Node
    {
        var point1 = DateTime.Now;
        AT.OnMainThread();

        GD.Print($"ThreadedInstantiateAsync Loading {resourcePath}");
        var loaded = await StartLoadingAsync<PackedScene>(resourcePath, validityCheckNode);

        GD.Print($"ThreadedInstantiateAsync DONE Loading {resourcePath}");

        var point2 = DateTime.Now;
        AT.OnMainThread();

        var monitor = EnsureExistingMonitor(validityCheckNode);

        if (monitor.InstantiateOnSeperateThread)
        {
            var ret = new TaskCompletionSource<T>();

            monitor.InstantationThreadInputQueue.Enqueue(new InstantiationThreadRequest
            {
                Input = loaded,
                MainThreadCallback = (res) =>
                {
                    if (ret.Task.IsCompleted) return;

                    AT.OnMainThread();
                    var point3 = DateTime.Now;

                    GD.Print($"ThreadedInstantiateAsync Success {point2 - point1} {point3 - point2}");

                    ret.SetResult((T)res);
                },
                MainThreadErrorHandler = (err) =>
                {
                    if (ret.Task.IsCompleted) return;

                    ret.SetException(err);
                },
                Output = null,
                ValidityCheckNode = validityCheckNode
            });

            return await ret.Task;
        }
        else
        {
            return loaded.Instantiate<T>();
        }
    }
}