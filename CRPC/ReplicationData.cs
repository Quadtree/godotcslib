using Godot;
using System.Collections.Generic;
using System;

public sealed class ReplicationData
{
    public int Id;

    public int TypeId;

    public int NetworkMaster;

    public List<byte[]> FieldValues;
}