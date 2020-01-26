using Godot;

public interface IReplicableFactory<R>
{
    IReplicable<R> CreateFrom(Node root, R replicationData);
}