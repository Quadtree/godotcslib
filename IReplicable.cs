using Godot;
using System.Collections.Generic;
using Godot.Collections;

public interface IReplicable<R>
{
    R GetReplicationData();
    void SetReplicationData(R data);

    int Id {get; set; }

    void Init();
}