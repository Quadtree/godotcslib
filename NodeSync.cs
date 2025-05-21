

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class NodeSync
{
    public interface HasId<I>
    {
        I Id { get; }
    }

    public static void Sync<T, R, I>(IEnumerable<T> source, Node targetParent, Func<T, R> nodeCreator, string overrideSyncType = null)
     where T : HasId<I>
     where R : Node, HasId<I>
    {
        var syncType = overrideSyncType ?? typeof(T).FullName;

        var existingInDest = targetParent.FindChildrenByType<R>().Where(it => it.GetMeta("sync_type", "").AsString() == syncType).ToHashSet();
        var existingInDestIds = existingInDest.Select(it => it.Id).ToHashSet();
        var sourceSet = source.ToHashSet();
        var sourceIds = source.Select(it => it.Id).ToHashSet();

        // to create
        foreach (var it in sourceSet.Where(it => !existingInDestIds.Contains(it.Id)))
        {
            var newNode = nodeCreator(it);
            newNode.SetMeta("sync_type", syncType);
            targetParent.AddChild(newNode);
            existingInDest.Add(newNode);
        }

        // to delete
        foreach (var it in existingInDest.Where(it => !sourceIds.Contains(it.Id)))
        {
            targetParent.RemoveChild(it);
        }
    }

    public static void SyncMultiMesh<T>(IEnumerable<T> source, MultiMesh target, Func<T, Transform3D> transformCreator)
    {
        if (target.InstanceCount == 0)
        {
            target.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
            target.VisibleInstanceCount = 0;
            target.InstanceCount = 1;
        }

        var sourceSet = source.ToList();

        while (target.InstanceCount < sourceSet.Count)
        {
            target.InstanceCount *= 2;
            target.VisibleInstanceCount = 0;
        }

        target.VisibleInstanceCount = sourceSet.Count;
        var idx = 0;

        foreach (var it in sourceSet)
        {
            target.SetInstanceTransform(idx, transformCreator(it));
            idx++;
        }
    }
}