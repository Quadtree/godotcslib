using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Godot;

[HasVersion(1)]
[DataContract]
public class NodeSaveState
{
    [DataMember] public Dictionary<string, NodeSaveElement> Data = new Dictionary<string, NodeSaveElement>();

    public List<Type> DynamicDependsOnList { get; set; }

    public string RootSceneFileName;

    public IEnumerable<Type> DynamicDependsOn => DynamicDependsOnList;
}

[DataContract]
public class NodeSaveElement
{
    [DataMember] public string ScenePath;
    [DataMember] public Dictionary<string, object> Data;
}

public class NodeSavable : Attribute
{
    public string SceneFilePath;
    public string[] ExtraProperties;

    public NodeSavable(string sceneFilePath = null, params string[] extraProperties)
    {
        ExtraProperties = extraProperties ?? new string[0];
        SceneFilePath = sceneFilePath;
    }
}

public class Hydrator : Node
{
    NodeSaveState State;

    public Hydrator(NodeSaveState state)
    {
        this.State = state;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (GetTree().CurrentScene != null)
        {
            GD.Print($"Current scene has exited the tree, current scene is now {GetTree().CurrentScene}");

            NodeStateSerializer.DoHydrate(GetTree().CurrentScene, State);
            QueueFree();
        }
    }
}

public static class NodeStateSerializer
{
    public static Tuple<NodeSaveState, ISet<Type>> Save(Node scene, string rootSceneFileName)
    {
        var dataToSave = new NodeSaveState();
        var types = new HashSet<Type>();

        dataToSave.RootSceneFileName = rootSceneFileName;

        foreach (var it in scene.FindChildrenByType<Node>().Where(it => Attribute.GetCustomAttribute(it.GetType(), typeof(NodeSavable)) != null))
        {
            GD.Print($"Serializing {it}");
            dataToSave.Data[GetFullyQualifiedNodePath(it)] = new NodeSaveElement { Data = ExtractNodeData(it, types), ScenePath = GetSceneFilePath(it) };
        }

        return Tuple.Create(dataToSave, (ISet<Type>)types.Where(it => it != null).ToHashSet());
    }

    public static void Load(Node contextNode, NodeSaveState state)
    {
        var tree = contextNode.GetTree();

        GD.Print($"Loading game. Current scene is {tree.CurrentScene}");
        tree.ChangeScene(state.RootSceneFileName);

        var hydrator = new Hydrator(state);
        contextNode.GetTree().Root.AddChild(hydrator);
    }

    public static void DoHydrate(Node contextNode, NodeSaveState state)
    {
        foreach (var it in state.Data.OrderBy(it => it.Key.Length))
        {
            var targetObject = GetNodeByAbsolutePath(contextNode, it.Key);

            if (targetObject == null)
            {
                var pathParts = it.Key.Split('/');
                var targetObjectParentPath = string.Join("/", pathParts.Take(pathParts.Length - 1));

                var targetObjectParent = GetNodeByAbsolutePath(contextNode, targetObjectParentPath);
                if (targetObjectParent == null)
                {
                    GD.Print($"Object {it.Key} was mentioned in a save, but we can't create it as we can't find its parent");
                    continue;
                }

                targetObject = GD.Load<PackedScene>(it.Value.ScenePath).Instance<Node>();
                targetObjectParent.AddChild(targetObject);
                GD.Print($"Created new object from save data: {GetFullyQualifiedNodePath(targetObject)}");
            }
        }

        foreach (var it in state.Data.OrderBy(it => it.Key.Length))
        {
            var targetObject = GetNodeByAbsolutePath(contextNode, it.Key);
            PushNodeData(targetObject, it.Value.Data);
            GD.Print($"Finished hydrating {GetFullyQualifiedNodePath(targetObject)}");
        }
    }

    private static Dictionary<string, object> ExtractNodeData(Node node, ISet<Type> typeSet)
    {
        var ret = new Dictionary<string, object>();
        var extraProps = (Attribute.GetCustomAttribute(node.GetType(), typeof(NodeSavable)) as NodeSavable)?.ExtraProperties ?? new string[0];

        Action<object, string, Type> saveDataElement = (object data, string propName, Type propType) =>
        {
            if (data is Node)
            {
                data = GetFullyQualifiedNodePath((Node)data);
            }

            ret[propName] = data;
            typeSet.Add(ret[propName]?.GetType() ?? propType);
        };

        foreach (var prop in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (extraProps.Contains(prop.Name) || Attribute.GetCustomAttribute(prop, typeof(NodeSavable)) != null)
            {
                saveDataElement(prop.GetMethod.Invoke(node, new object[0]), prop.Name, prop.PropertyType);
            }
        }

        foreach (var prop in node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (extraProps.Contains(prop.Name) || Attribute.GetCustomAttribute(prop, typeof(NodeSavable)) != null)
            {
                saveDataElement(prop.GetValue(node), prop.Name, prop.FieldType);
            }
        }

        return ret;
    }

    private static void PushNodeData(Node node, Dictionary<string, object> data)
    {
        var extraProps = (Attribute.GetCustomAttribute(node.GetType(), typeof(NodeSavable)) as NodeSavable)?.ExtraProperties ?? new string[0];

        Action<object, Action<object>, Type> pushDataElement = (object currentData, Action<object> target, Type propType) =>
        {
            if (propType.IsSubclassOf(typeof(Node)))
            {
                currentData = GetNodeByAbsolutePath(node, (string)currentData);
            }

            target(currentData);
        };

        foreach (var prop in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (extraProps.Contains(prop.Name) || Attribute.GetCustomAttribute(prop, typeof(NodeSavable)) != null)
            {
                pushDataElement(data[prop.Name], (subData) => prop.SetMethod.Invoke(node, new object[] { subData }), prop.PropertyType);
            }
        }

        foreach (var prop in node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (extraProps.Contains(prop.Name) || Attribute.GetCustomAttribute(prop, typeof(NodeSavable)) != null)
            {
                pushDataElement(data[prop.Name], (subData) => prop.SetValue(node, subData), prop.FieldType);
            }
        }
    }

    private static Node GetNodeByAbsolutePath(Node contextNode, string path)
    {
        if (path?.Length > 0)
            return contextNode.GetTree().Root.GetNode(path);
        else
            return null;
    }

    private static string GetSceneFilePath(Node node)
    {
        var attrib = ((NodeSavable)Attribute.GetCustomAttribute(node.GetType(), typeof(NodeSavable)));
        if (attrib == null) GD.PushError($"Unable to find scene file path for {node}");
        return attrib.SceneFilePath;
    }

    private static string GetFullyQualifiedNodePath(Node node)
    {
        var ret = "";

        while (node != null)
        {
            ret = $"/{node.Name}{ret}";
            node = node.GetParent();
        }

        return ret;
    }
}