using Godot;
using System.Collections.Generic;
using System;

public class ReplicationData
{
    public int Id;

    public int TypeId;

    class SpawnableType
    {
        public Type Clazz;
        public string SceneFileName;
    }

    static List<SpawnableType> KnownTypes = new List<SpawnableType>();
    static Dictionary<Type, int> TypeToTypeIdMapping = new Dictionary<Type, int>();

    public static void RegisterType(Type type, string sceneFileName)
    {
        KnownTypes.Add(new SpawnableType(){
            Clazz = type,
            SceneFileName = sceneFileName,
        });
        TypeToTypeIdMapping.Add(type, KnownTypes.Count - 1);
    }

    public static int GetTypeId(Type type)
    {
        return TypeToTypeIdMapping[type];
    }

    public Node CreateNew()
    {
        
    }
}