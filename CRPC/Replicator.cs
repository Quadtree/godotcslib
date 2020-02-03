/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

static class Replicator
{
    public static ReplicationData GetReplicationDataFrom(this IReplicable replicable, int typeId)
    {
        ReplicationData ret = new ReplicationData(){
            Id = replicable.Id,
            TypeId = typeId,
            NetworkMaster = ((Node)replicable).GetNetworkMaster(),
            FieldValues = new List<byte[]>(),
        };

        foreach (var prop in replicable.GetType().GetProperties())
        {
            if (prop.GetCustomAttribute<Replicated>() != null)
            {
                ret.FieldValues.Add(Util.ObjToBytes(prop.GetValue(replicable), prop.PropertyType));
            }
        }

        foreach (var prop in replicable.GetType().GetFields())
        {
            if (prop.GetCustomAttribute<Replicated>() != null)
            {
                ret.FieldValues.Add(Util.ObjToBytes(prop.GetValue(replicable), prop.FieldType));
            }
        }

        return ret;
    }

    public static void SetReplicationDataTo(this IReplicable replicable, ReplicationData data)
    {
        replicable.Id = data.Id;
        ((Node)replicable).SetNetworkMaster(data.NetworkMaster);

        int i = 0;

        foreach (var prop in replicable.GetType().GetProperties())
        {
            if (prop.GetCustomAttribute<Replicated>() != null)
            {
                prop.SetValue(replicable, Util.BytesToObj(data.FieldValues[i++], prop.PropertyType));
            }
        }

        foreach (var prop in replicable.GetType().GetFields())
        {
            if (prop.GetCustomAttribute<Replicated>() != null)
            {
                prop.SetValue(replicable, Util.BytesToObj(data.FieldValues[i++], prop.FieldType));
            }
        }
    }
}