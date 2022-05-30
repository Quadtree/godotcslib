/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public static class Util
{
    private const bool SERIALIZATION_DEBUG_PRINT = false;

    public static readonly RandomNumberGenerator rng = new RandomNumberGenerator();

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

    public static T FindChildByPredicate<T>(this Node node, Predicate<T> predicate, int maxRecursionDepth = 10) where T : Node
    {
        var c = node.GetChildCount();
        for (int i = 0; i < c; ++i)
        {
            var n = node.GetChild(i);
            if (n is T)
            {
                if (predicate.Invoke((T)n)) return (T)n;
            }
        }

        if (maxRecursionDepth > 0)
        {
            for (int i = 0; i < c; ++i)
            {
                var n = node.GetChild(i);
                var ret = n.FindChildByPredicate<T>(predicate);
                if (ret != null) return ret;
            }
        }

        return null;
    }

    private static ConditionalWeakTable<Node, System.Collections.Generic.Dictionary<Type, Node>> findChildByTypeCache = new ConditionalWeakTable<Node, System.Collections.Generic.Dictionary<Type, Node>>();

    public static T FindChildByType<T>(this Node node, int maxRecursionDepth = 10) where T : Node
    {
        if (node == null) return null;

        var dict = findChildByTypeCache.GetOrCreateValue(node);

        if (dict.ContainsKey(typeof(T)) && dict[typeof(T)].IsInstanceValid())
        {
            return (T)dict[typeof(T)];
        }

        var c = node.GetChildCount();
        for (int i = 0; i < c; ++i)
        {
            var n = node.GetChild(i);

            if (n is T)
            {
                dict[typeof(T)] = (T)n;
                return (T)n;
            }
        }

        if (maxRecursionDepth > 0)
        {
            for (int i = 0; i < c; ++i)
            {
                var n = node.GetChild(i);
                var ret = n.FindChildByType<T>(maxRecursionDepth - 1);
                if (ret != null) return ret;
            }
        }

        return null;
    }

    public static IEnumerable<T> FindChildrenByType<T>(this Node node, int maxRecursionDepth = 10) where T : class
    {
        var c = node.GetChildCount();
        for (int i = 0; i < c; ++i)
        {
            var n = node.GetChild(i);

            if (n is T)
            {
                yield return ((T)(object)n);
            }

            if (maxRecursionDepth > 0)
            {
                foreach (var nn in n.FindChildrenByType<T>(maxRecursionDepth - 1))
                {
                    yield return nn;
                }
            }
        }
    }

    private static ConditionalWeakTable<Node, System.Collections.Generic.Dictionary<string, Node>> findChildByNameCache = new ConditionalWeakTable<Node, System.Collections.Generic.Dictionary<string, Node>>();

    public static T FindChildByName<T>(this Node node, string name, int maxRecursionDepth = 10) where T : Node
    {
        if (node == null || name == null) return null;

        var dict = findChildByNameCache.GetOrCreateValue(node);

        if (dict.ContainsKey(name) && dict[name].IsInstanceValid())
        {
            return (T)dict[name];
        }

        var c = node.GetChildCount();
        for (int i = 0; i < c; ++i)
        {
            var n = node.GetChild(i);

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
        }

        if (maxRecursionDepth > 0)
        {
            for (int i = 0; i < c; ++i)
            {
                var n = node.GetChild(i);
                var ret = n.FindChildByName<T>(name, maxRecursionDepth - 1);
                if (ret != null) return ret;
            }
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
        if (type == typeof(System.Int32)) { WriteAll(mem, BitConverter.GetBytes((int)obj)); return; }
        if (type == typeof(long)) { WriteAll(mem, BitConverter.GetBytes((long)obj)); return; }
        if (type == typeof(float))
        {
            //Console.WriteLine($"It is a {type} / {obj.GetType()}");
            SerializationLog($"The single is {(float)obj}");
            WriteAll(mem, BitConverter.GetBytes((float)obj));
            return;
        }
        if (type == typeof(double)) { WriteAll(mem, BitConverter.GetBytes((double)obj)); return; }
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
            WriteAll(mem, new byte[] { (byte)obj });
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

            for (int i = 0; i < len; ++i)
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

            for (int i = 0; i < len; ++i)
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

    [Obsolete("Use Random() instead")]
    public static float random()
    {
        return (float)rand.NextDouble();
    }

    public static float Random()
    {
        return (float)rand.NextDouble();
    }

    public static float RandF(float min, float max)
    {
        return Random() * (max - min) + min;
    }

    public static bool RandChance(float chance)
    {
        return Random() <= chance;
    }

    public static bool RandChanceMil(int chancePerMil)
    {
        return RandInt(0, 1000) < chancePerMil;
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
        var control = node.GetTree().CurrentScene.FindChildByPredicate<Control>(it => it.HasFocus());

        return control != null;
    }

    public static T FindParentByType<T>(this Node node)
    {
        while (true)
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
        contextNode.GetTree().CurrentScene.AddChild(particles);

        particles.SetGlobalLocation(location);

        particles.OneShot = true;
        particles.Emitting = true;

        var timer = new Timer();
        timer.Autostart = true;
        timer.WaitTime = 5;
        timer.Connect("timeout", particles, "queue_free");
        particles.AddChild(timer);
    }

    public static void SpawnOneShotCPUParticleSystem(PackedScene system, Node contextNode, Vector3 location)
    {
        if (system == null) return;

        var particles = (CPUParticles)system.Instance();
        contextNode.GetTree().CurrentScene.AddChild(particles);

        particles.SetGlobalLocation(location);

        particles.OneShot = true;
        particles.Emitting = true;

        var timer = new Timer();
        timer.Autostart = true;
        timer.WaitTime = 5;
        timer.Connect("timeout", particles, "queue_free");
        particles.AddChild(timer);
    }

    public static void SpawnOneShotSound(string resName, Node contextNode, Vector3 location)
    {
        Util.SpawnOneShotSound((AudioStream)GD.Load(resName), contextNode, location);
    }

    public static void SpawnOneShotSound(AudioStream sample, Node contextNode, Vector3 location)
    {
        if (sample == null) return;

        var r = contextNode.GetTree().CurrentScene;
        var c = r.GetChildCount();

        var existingCount = 0;
        AudioStreamPlayer3D availExisting = null;

        for (int i = 0; i < c; ++i)
        {
            var n = r.GetChild(i);
            if (n is AudioStreamPlayer3D)
            {
                existingCount++;

                if (!((AudioStreamPlayer3D)n).IsPlaying())
                {
                    availExisting = (AudioStreamPlayer3D)n;
                    break;
                }
            }
        }

        if (availExisting == null && existingCount < 10)
        {
            availExisting = new AudioStreamPlayer3D();
            contextNode.GetTree().CurrentScene.AddChild(availExisting);
        }

        if (availExisting != null)
        {
            availExisting.SetGlobalLocation(location);
            availExisting.Stream = sample;
            availExisting.UnitDb = 15;
            availExisting.Play();
        }
    }

    public static void SpawnOneShotSound(string resName, Node contextNode, float volumeOffset = 0.0f)
    {
        Util.SpawnOneShotSound((AudioStream)GD.Load(resName), contextNode, volumeOffset);
    }

    public static void SpawnOneShotSound(AudioStream sample, Node contextNode, float volumeOffset = 0.0f)
    {
        if (sample == null) return;

        var r = contextNode.GetTree().CurrentScene;
        var c = r.GetChildCount();

        var existingCount = 0;
        AudioStreamPlayer availExisting = null;

        for (int i = 0; i < c; ++i)
        {
            var n = r.GetChild(i);
            if (n is AudioStreamPlayer)
            {
                existingCount++;

                if (!((AudioStreamPlayer)n).IsPlaying())
                {
                    availExisting = (AudioStreamPlayer)n;
                    break;
                }
            }
        }

        if (availExisting == null && existingCount < 10)
        {
            availExisting = new AudioStreamPlayer();
            contextNode.GetTree().CurrentScene.AddChild(availExisting);
        }

        if (availExisting != null)
        {
            availExisting.Stream = sample;
            availExisting.VolumeDb = volumeOffset;
            availExisting.Play();
        }
    }

    public static void SpeedUpPhysicsIfNeeded()
    {
        Engine.IterationsPerSecond = Math.Max(Engine.IterationsPerSecond, (int)Engine.GetFramesPerSecond());
    }

    public static T Clamp<T>(T initial, T min, T max) where T : IComparable<T>
    {
        if (initial.CompareTo(max) > 0) initial = max;
        if (initial.CompareTo(min) < 0) initial = min;
        return initial;
    }

    public static string TitleCase(this string str)
    {
        return str.Substr(0, 1).ToUpper() + str.Substr(1, 1000).ToLower();
    }

    public static T Choice<T>(IReadOnlyList<T> list)
    {
        if (list.Count == 0) return default(T);
        return list[rng.RandiRange(0, list.Count - 1)];
    }

    public static T Choice<T>(IEnumerable<T> enumerable)
    {
        var n = 0;
        var ret = default(T);

        foreach (var it in enumerable)
        {
            if (RandInt(0, ++n) == 0) ret = it;
        }

        return ret;
    }

    public static T MinBy<T, R>(this IEnumerable<T> ie, Func<T, R> func) where R : IComparable<R>
    {
        bool hasMin = false;
        var minComp = default(R);
        var minVal = default(T);

        foreach (var val in ie)
        {
            var comp = func(val);

            if (!hasMin || comp.CompareTo(minComp) < 0)
            {
                hasMin = true;
                minComp = comp;
                minVal = val;
            }
        }

        return minVal;
    }

    public static T MaxBy<T, R>(this IEnumerable<T> ie, Func<T, R> func) where R : IComparable<R>
    {
        bool hasMax = false;
        var minComp = default(R);
        var minVal = default(T);

        foreach (var val in ie)
        {
            var comp = func(val);

            if (!hasMax || comp.CompareTo(minComp) > 0)
            {
                hasMax = true;
                minComp = comp;
                minVal = val;
            }
        }

        return minVal;
    }

    public static IEnumerable<T> Single<T>(T v)
    {
        yield return v;
    }

    public static void TakeScreenshot(Node ctx)
    {
        var image = ctx.GetViewport().GetTexture().GetData();
        image.FlipY();
        var dir = new Godot.Directory();
        dir.Open("user://");
        dir.MakeDir("screenshots");

        image.SavePng($"user://screenshots/{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.png");
    }

    public static int Square(int n)
    {
        return n * n;
    }

    public static IReadOnlyCollection<T> GetEnumValues<T>() where T : Enum
    {
        return (IReadOnlyCollection<T>)Enum.GetValues(typeof(T));
    }

    // There is a bug in Godot where if you use ContinueWith() in HTML5 it will crash
    // This function can be used until the bug is likely fixed in 4.0
    public static void SafeContinueWith(this Task task, Action callback)
    {
        _SafeContinueWith(task, callback);
    }

    private static async Task _SafeContinueWith(Task task, Action callback)
    {
        await task;
        if (callback != null) callback();
    }
}