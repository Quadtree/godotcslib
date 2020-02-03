using Godot;
using System.Collections.Generic;
using Godot.Collections;

public interface IReplicable
{
    int Id {get; set; }

    void Init();
}