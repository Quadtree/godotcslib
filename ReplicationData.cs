using Godot;
using System.Collections.Generic;
using System;

public class ReplicationData
{
    public int Id;

    public int TypeId;

    public List<byte[]> FieldValues;

    class SpawnableType
    {
        public Type Clazz;
        public PackedScene Scene;
    }

    static List<SpawnableType> KnownTypes = new List<SpawnableType>();
    static Dictionary<Type, int> TypeToTypeIdMapping = new Dictionary<Type, int>();

    public static void RegisterType(Type type, string sceneFileName)
    {
        KnownTypes.Add(new SpawnableType(){
            Clazz = type,
            Scene = (PackedScene)ResourceLoader.Load(sceneFileName),
        });
        TypeToTypeIdMapping.Add(type, KnownTypes.Count - 1);
    }

    public static int GetTypeId(Type type)
    {
        return TypeToTypeIdMapping[type];
    }

    public Node CreateNew()
    {
        return KnownTypes[TypeId].Scene.Instance();
    }
}