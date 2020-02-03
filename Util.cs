/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;
using System.IO;
using System.Text;

public static class Util
{
    private const bool SERIALIZATION_DEBUG_PRINT = false;

    public static List<T> ToList<T>(this Godot.Collections.Array array)
    {
        var ret = new List<T>();

        foreach (var v in array)
        {
            if (v is T) ret.Add((T)v);
        }

        return ret;
    }

    public static HashSet<T> ToHashSet<T>(this Godot.Collections.Array array)
    {
        var ret = new HashSet<T>();

        foreach (var v in array)
        {
            ret.Add((T)v);
        }

        return ret;
    }

    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> array)
    {
        var ret = new HashSet<T>();

        foreach (var v in array)
        {
            ret.Add(v);
        }

        return ret;
    }

    public static Godot.Collections.Array VecToArray(Vector2 vector2)
    {
        return new Godot.Collections.Array(){
            vector2.x,
            vector2.y
        };
    }

    public static Vector2 ArrayToVec(Godot.Collections.Array array)
    {
        return new Vector2(
            (float)array[0],
            (float)array[1]
        );
    }

    public static T FindChildByPredicate<T>(this Node node, Predicate<T> predicate) where T : class
    {
        foreach (var n in node.GetChildren().ToList<Node>())
        {
            if (n is T)
            {
                if (predicate.Invoke((T)(object)n)) return (T)(object)n;
            }

            var ret = n.FindChildByPredicate<T>(predicate);
            if (ret != null) return ret;
        }

        return null;
    }

    public static T FindChildByType<T>(this Node node) where T : class
    {
        foreach (var n in node.GetChildren().ToList<Node>())
        {
            if (n is T)
            {
                return (T)(object)n;
            }

            var ret = n.FindChildByType<T>();
            if (ret != null) return ret;
        }

        return null;
    }

    public static T FindChildByName<T>(this Node node, string name) where T : Node
    {
        foreach (var n in node.GetChildren().ToList<Node>())
        {
            if (n.Name == name)
            {
                if (n is T)
                {
                    return (T)n;
                }
                else
                {
                    Console.WriteLine($"Node {name} is of unexpected type {n.GetType()}");
                }
            }

            var ret = n.FindChildByName<T>(name);
            if (ret != null) return ret;
        }

        return null;
    }


    public static byte[] ObjToBytes<T>(T obj)
    {
        MemoryStream ms = new MemoryStream();
        ObjToBytes(obj, typeof(T), ms);
        return ms.ToArray();
    }

    public static byte[] ObjToBytes(object obj, Type type)
    {
        MemoryStream ms = new MemoryStream();
        ObjToBytes(obj, type, ms);
        return ms.ToArray();
    }

    private static void WriteAll(MemoryStream memoryStream, byte[] bytes)
    {
        memoryStream.Write(bytes, 0, bytes.Length);
    }

    private static void ObjToBytes(object obj, Type type, MemoryStream mem)
    {
        SerializationLog($"Serializing a {type}");
        if (type == typeof(System.Int32)){ WriteAll(mem, BitConverter.GetBytes((int)obj)); return; }
        if (type == typeof(long)){ WriteAll(mem, BitConverter.GetBytes((long)obj)); return; }
        if (type == typeof(float))
        {
            //Console.WriteLine($"It is a {type} / {obj.GetType()}");
            SerializationLog($"The single is {(float)obj}");
            WriteAll(mem, BitConverter.GetBytes((float)obj));
            return;
        }
        if (type == typeof(double)){ WriteAll(mem, BitConverter.GetBytes((double)obj)); return; }
        if (type.IsEnum)
        {
            WriteAll(mem, BitConverter.GetBytes((int)obj));
            return;
        }
        if (type == typeof(string))
        {
            if (obj == null)
            {
                WriteAll(mem, BitConverter.GetBytes(-1));
                return;
            }
            byte[] rawBytes = ((string)obj).ToUTF8();

            WriteAll(mem, BitConverter.GetBytes(rawBytes.Length));
            WriteAll(mem, rawBytes);
            return;
        }
        if (type == typeof(byte))
        {
            WriteAll(mem, new byte[]{ (byte)obj });
            return;
        }
        if (type == typeof(bool))
        {
            WriteAll(mem, BitConverter.GetBytes((bool)obj));
            return;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            WriteAll(mem, BitConverter.GetBytes(((System.Collections.IList)obj).Count));

            foreach (var o in ((System.Collections.IList)obj))
            {
                ObjToBytes(o, type.GenericTypeArguments[0], mem);
            }
            return;
        }
        if (type.IsArray)
        {
            if (obj == null)
            {
                WriteAll(mem, BitConverter.GetBytes(-1));
                return;
            }

            WriteAll(mem, BitConverter.GetBytes(((System.Collections.IList)obj).Count));

            foreach (var o in ((System.Collections.IList)obj))
            {
                ObjToBytes(o, type.GetElementType(), mem);
            }
            return;
        }

        SerializationLog($"Seems it's some kind of object");

        foreach (var field in obj.GetType().GetFields())
        {
            SerializationLog($"Descending into {field.Name}");
            object fieldValue = field.GetValue(obj);
            ObjToBytes(fieldValue, field.FieldType, mem);
        }
    }

    public static T BytesToObj<T>(byte[] bytes)
    {
        MemoryStream mem = new MemoryStream(bytes);
        return (T)BytesToObj(mem, typeof(T));
    }

    public static object BytesToObj(byte[] bytes, Type type)
    {
        MemoryStream mem = new MemoryStream(bytes);
        return BytesToObj(mem, type);
    }

    private static object BytesToObj(MemoryStream mem, Type type)
    {
        byte[] buffer = new byte[8];

        SerializationLog($"Deserializing a {type}");
        if (type == typeof(int))
        {
            mem.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }
        if (type == typeof(long))
        {
            mem.Read(buffer, 0, 8);
            return BitConverter.ToInt64(buffer, 0);
        }
        if (type == typeof(float))
        {
            mem.Read(buffer, 0, 4);
            SerializationLog($"The single is {BitConverter.ToSingle(buffer, 0)}");
            return BitConverter.ToSingle(buffer, 0);
        }
        if (type == typeof(int))
        {
            mem.Read(buffer, 0, 8);
            return BitConverter.ToDouble(buffer, 0);
        }
        if (type == typeof(byte))
        {
            mem.Read(buffer, 0, 1);
            return buffer[0];
        }
        if (type.IsEnum)
        {
            mem.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }
        if (type == typeof(string))
        {
            mem.Read(buffer, 0, 4);
            var len = BitConverter.ToInt32(buffer, 0);

            if (len == -1) return null;

            buffer = new byte[len];
            mem.Read(buffer, 0, len);

            return Encoding.UTF8.GetString(buffer);
        }
        if (type == typeof(bool))
        {
            mem.Read(buffer, 0, 1);
            return BitConverter.ToBoolean(buffer, 0);
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            mem.Read(buffer, 0, 4);
            var len = BitConverter.ToInt32(buffer, 0);

            System.Collections.IList ret = (System.Collections.IList)Activator.CreateInstance(type);

            for (int i=0;i<len;++i)
            {
                ret.Add(BytesToObj(mem, type.GenericTypeArguments[0]));
            }

            return ret;
        }
        if (type.IsArray)
        {
            mem.Read(buffer, 0, 4);
            var len = BitConverter.ToInt32(buffer, 0);

            System.Collections.IList ret = (System.Collections.IList)Activator.CreateInstance(type, len);

            for (int i=0;i<len;++i)
            {
                ret[i] = BytesToObj(mem, type.GetElementType());
            }

            return ret;
        }

        SerializationLog($"Seems it's some kind of object");

        if (!type.IsSealed) Console.WriteLine($"Warning: {type} should be sealed");

        var inst = Activator.CreateInstance(type);

        foreach (var field in type.GetFields())
        {
            SerializationLog($"Descending into {field.Name}");
            field.SetValue(inst, BytesToObj(mem, field.FieldType));
        }

        return inst;
    }

    public static string ToHex(this byte[] bytes)
    {
        StringBuilder ret = new StringBuilder();
        foreach (var b in bytes)
        {
            ret.AppendFormat("{0:x2} ", b);
        }

        return ret.ToString();
    }

    public static string ToMixedHex(this byte[] bytes)
    {
        StringBuilder ret = new StringBuilder();
        foreach (var b in bytes)
        {
            if ((b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z') || (b >= '0' && b <= '9') || b == ' ' || b == '_')
                ret.AppendFormat("{0} ", (char)b);
            else
                ret.AppendFormat("{0:x2} ", b);
        }

        return ret.ToString();
    }

    #pragma warning disable
    private static void SerializationLog(string txt)
    {
        if (SERIALIZATION_DEBUG_PRINT)
        {
            Console.WriteLine(txt);
        }
    }

    public static void AssertSenderIsMaster(this Node node)
    {
        if (node.GetTree().GetRpcSenderId() != 0 && node.GetTree().GetRpcSenderId() != node.GetNetworkMaster()) throw new Exception($"This function can only be called on the owner, expected {node.GetNetworkMaster()} got {node.GetTree().GetRpcSenderId()}");
    }

    public static void AssertSenderIsServer(this Node node)
    {
        if (node.GetTree().GetRpcSenderId() != 0 && node.GetTree().GetRpcSenderId() != 1) throw new Exception($"This function can only be called by the server, expected 1 got {node.GetTree().GetRpcSenderId()}");
    }

    public static void AssertSenderIsServerOrMaster(this Node node)
    {
        if (node.GetTree().GetRpcSenderId() != 0 && node.GetTree().GetRpcSenderId() != 1 && node.GetTree().GetRpcSenderId() != node.GetNetworkMaster()) throw new Exception($"This function can only be called by the server or master, expected 1 or {node.GetNetworkMaster()} got {node.GetTree().GetRpcSenderId()}");
    }

    private static Random rand = new Random();

    public static int RandInt(int minInclusive, int maxExclusive)
    {
        return rand.Next(minInclusive, maxExclusive);
    }

    public static bool IsServer(this Node node)
    {
        if (node.GetTree().NetworkPeer?.GetUniqueId() == 1) return true;

        return false;
    }

    public static bool IsClient(this Node node)
    {
        if (node.GetTree().NetworkPeer?.GetUniqueId() != 1) return true;

        return false;
    }

    public static float random()
    {
        return (float)rand.NextDouble();
    }

    public static Vector3 GetGlobalLocation(this Spatial node)
    {
        return node.GlobalTransform.origin;
    }

    public static void SetGlobalLocation(this Spatial node, Vector3 globalLocation)
    {
        var t = node.GlobalTransform;
        t.origin = globalLocation;
        node.GlobalTransform = t;
    }

    public static void CreateRegularTimer(this Node node, string targetMethodName, float interval, Timer.TimerProcessMode mode = Timer.TimerProcessMode.Idle)
    {
        Timer uploadTimer = new Timer();
        uploadTimer.OneShot = false;
        uploadTimer.Autostart = true;
        uploadTimer.Connect("timeout", node, targetMethodName);
        uploadTimer.WaitTime = interval;
        uploadTimer.ProcessMode = mode;
        node.AddChild(uploadTimer);
    }

    /**
     * Determines if this object is still valid (aka it has not been disposed)
     * Because Godot uses reference counting, this should always be called on objects that are
     * referenced from other objects
     */
    public static bool IsInstanceValid(this Godot.Object obj)
    {
        return Godot.Object.IsInstanceValid(obj);
    }

    /**
     * Determines if the UI has the focus. Generally in game input shouldn't happen
     * as long as the UI has focus
     */
    public static bool IsUIFocused(this Node node)
    {
        var control = node.GetTree().Root.FindChildByPredicate<Control>(it => it.HasFocus());

        return control != null;
    }

    public static void CRPC(this Node node, string methodName, params object[] parameters)
    {
        node.GetTree().Root.FindChildByType<NetworkController>().SendCRPC(node, methodName, parameters);
    }

    public static T FindParentByType<T>(this Node node)
    {
        while(true)
        {
            var parent = node.GetParentOrNull<Node>();

            if (parent == null) return default(T);
            if (parent is T)
            {
                return (T)(object)node.GetParentOrNull<Node>();
            }
            node = parent;
        }
    }

    public static void SpawnOneShotParticleSystem(PackedScene system, Node contextNode, Vector3 location)
    {
        if (system == null) return;

        var particles = (Particles)system.Instance();
        contextNode.GetTree().Root.AddChild(particles);

        particles.SetGlobalLocation(location);

        particles.OneShot = true;
        particles.Emitting = true;

        var timer = new Timer();
        timer.Autostart = true;
        timer.WaitTime = 5;
        timer.Connect("timeout", particles, "queue_free");
        particles.AddChild(timer);

        var n = contextNode.GetTree().Root.GetChildren().ToList<Particles>().Count;

        Console.WriteLine($"N={n}");
    }
}