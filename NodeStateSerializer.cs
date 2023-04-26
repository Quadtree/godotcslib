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
}

[DataContract]
public class NodeSaveElement
{
    [DataMember] public string ScenePath;
    [DataMember] public Dictionary<string, object> Data;
}

public class NodeSavable : Attribute
{
    public string[] ExtraProperties;

    public NodeSavable(params string[] extraProperties)
    {
        ExtraProperties = extraProperties ?? new string[0];
    }
}

public static class NodeStateSerializer
{
    public static Tuple<NodeSaveState, ISet<Type>> Save(Node scene)
    {
        var dataToSave = new NodeSaveState();
        var types = new HashSet<Type>();

        foreach (var it in scene.FindChildrenByType<Node>().Where(it => Attribute.GetCustomAttribute(it.GetType(), typeof(NodeSavable)) != null))
        {
            GD.Print($"Serializing {it}");
            dataToSave.Data[it.GetFullyQualifiedNodePath()] = new NodeSaveElement { Data = ExtractNodeData(it, types), ScenePath = it.SceneFilePath };
        }

        return Tuple.Create(dataToSave, (ISet<Type>)types.Where(it => it != null).ToHashSet());
    }

    public static void Load(Node contextNode, NodeSaveState state)
    {
        var tree = contextNode.GetTree();

        GD.Print($"Loading game. Current scene is {tree.CurrentScene}");
        tree.ChangeSceneToFile(tree.CurrentScene.SceneFilePath);

        Action frameProcessor = null;

        frameProcessor = () =>
        {
            if (tree.CurrentScene != null)
            {
                GD.Print($"Current scene has exited the tree, current scene is now {tree.CurrentScene}");
                tree.ProcessFrame -= frameProcessor;

                DoHydrate(tree.CurrentScene, state);
            }
        };

        tree.ProcessFrame += frameProcessor;
    }

    private static void DoHydrate(Node contextNode, NodeSaveState state)
    {
        foreach (var it in state.Data.OrderBy(it => it.Key.Length))
        {
            var targetObject = GetNodeByAbsolutePath(contextNode, it.Key);

            if (targetObject == null)
            {
                var pathParts = it.Key.Split('/');
                var targetObjectParentPath = string.Join('/', pathParts.Take(pathParts.Length - 1));

                var targetObjectParent = GetNodeByAbsolutePath(contextNode, targetObjectParentPath);
                if (targetObjectParent == null)
                {
                    GD.Print($"Object {it.Key} was mentioned in a save, but we can't create it as we can't find its parent");
                    continue;
                }

                targetObject = GD.Load<PackedScene>(it.Value.ScenePath).Instantiate<Node>();
                targetObjectParent.AddChild(targetObject);
                GD.Print($"Created new object from save data: {targetObject.GetFullyQualifiedNodePath()}");
            }
        }

        foreach (var it in state.Data.OrderBy(it => it.Key.Length))
        {
            var targetObject = GetNodeByAbsolutePath(contextNode, it.Key);
            PushNodeData(targetObject, it.Value.Data);
            GD.Print($"Finished hydrating {targetObject.GetFullyQualifiedNodePath()}");
        }
    }

    private static Dictionary<string, object> ExtractNodeData(Node node, ISet<Type> typeSet)
    {
        var ret = new Dictionary<string, object>();
        var extraProps = (Attribute.GetCustomAttribute(node.GetType(), typeof(NodeSavable)) as NodeSavable)?.ExtraProperties ?? new string[0];

        var saveDataElement = (object data, string propName, Type propType) =>
        {
            if (data is Node)
            {
                data = ((Node)data).GetFullyQualifiedNodePath();
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

        var pushDataElement = (object data, Action<object> target, Type propType) =>
        {
            if (propType.IsSubclassOf(typeof(Node)))
            {
                data = GetNodeByAbsolutePath(node, (string)data);
            }

            target(data);
        };

        foreach (var prop in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (extraProps.Contains(prop.Name) || Attribute.GetCustomAttribute(prop, typeof(NodeSavable)) != null)
            {
                pushDataElement(data[prop.Name], (data) => prop.SetMethod.Invoke(node, new object[] { data }), prop.PropertyType);
            }
        }

        foreach (var prop in node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (extraProps.Contains(prop.Name) || Attribute.GetCustomAttribute(prop, typeof(NodeSavable)) != null)
            {
                pushDataElement(data[prop.Name], (data) => prop.SetValue(node, data), prop.FieldType);
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
}