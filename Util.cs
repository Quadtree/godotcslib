/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public static class Util
{
    private const bool SERIALIZATION_DEBUG_PRINT = false;

    public static readonly RandomNumberGenerator rng = new RandomNumberGenerator();

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
            vector2.X,
            vector2.Y
        };
    }

    public static Vector2 ArrayToVec(Godot.Collections.Array array)
    {
        return new Vector2(
            (float)array[0],
            (float)array[1]
        );
    }

    private static ConditionalWeakTable<Node, System.Collections.Generic.Dictionary<object, Node>> findChildByPredicateCache = new ConditionalWeakTable<Node, System.Collections.Generic.Dictionary<object, Node>>();

    public static T FindChildByPredicate<T>(this Node node, Predicate<T> predicate, int maxRecursionDepth = 10, object immutableCacheKey = null, Node initialNode = null) where T : Node
    {
        if (node == null) return null;

        if (immutableCacheKey != null && initialNode == null)
        {
            initialNode = node;

            var dict = findChildByPredicateCache.GetOrCreateValue(node);

            Node ret;
            if (dict.TryGetValue(immutableCacheKey, out ret) && ret.IsInstanceValid() && ret.IsInsideTree())
            {
                return (T)ret;
            }
        }

        var c = node.GetChildCount();
        for (int i = 0; i < c; ++i)
        {
            var n = node.GetChild(i);
            if (n is T)
            {
                if (predicate.Invoke((T)n))
                {
                    if (immutableCacheKey != null)
                    {
                        var dict = findChildByPredicateCache.GetOrCreateValue(initialNode);
                        dict[immutableCacheKey] = (T)n;
                    }
                    return (T)n;
                }
            }
        }

        if (maxRecursionDepth > 0)
        {
            for (int i = 0; i < c; ++i)
            {
                var n = node.GetChild(i);
                var ret = n.FindChildByPredicate<T>(predicate, maxRecursionDepth - 1, immutableCacheKey, initialNode);
                if (ret != null) return ret;
            }
        }

        return null;
    }

    private static ConditionalWeakTable<Node, System.Collections.Generic.Dictionary<Type, Node>> findChildByTypeCache = new ConditionalWeakTable<Node, System.Collections.Generic.Dictionary<Type, Node>>();

    public static T FindChildByType<T>(this Node node, int maxRecursionDepth = 10, Node initialNode = null) where T : Node
    {
        if (node == null) return null;

        if (initialNode == null)
        {
            initialNode = node;

            var dict = findChildByTypeCache.GetOrCreateValue(node);

            Node ret;
            if (dict.TryGetValue(typeof(T), out ret) && ret.IsInstanceValid() && ret.IsInsideTree())
            {
                return (T)ret;
            }
        }

        var c = node.GetChildCount();
        for (int i = 0; i < c; ++i)
        {
            var n = node.GetChild(i);

            if (n is T)
            {
                var dict = findChildByTypeCache.GetOrCreateValue(initialNode);
                dict[typeof(T)] = (T)n;
                return (T)n;
            }
        }

        if (maxRecursionDepth > 0)
        {
            for (int i = 0; i < c; ++i)
            {
                var n = node.GetChild(i);
                var ret = n.FindChildByType<T>(maxRecursionDepth - 1, initialNode);
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

    public static IEnumerable<T> FindChildrenByPredicate<T>(this Node node, Func<T, bool> predicate, int maxRecursionDepth = 10) where T : class
    {
        var c = node.GetChildCount();
        for (int i = 0; i < c; ++i)
        {
            var n = node.GetChild(i);

            if (n is T typedNode)
            {
                if (predicate(typedNode)) yield return typedNode;
            }

            if (maxRecursionDepth > 0)
            {
                foreach (var nn in n.FindChildrenByPredicate<T>(predicate, maxRecursionDepth - 1))
                {
                    yield return nn;
                }
            }
        }
    }

    private static ConditionalWeakTable<Node, System.Collections.Generic.Dictionary<string, Node>> findChildByNameCache = new ConditionalWeakTable<Node, System.Collections.Generic.Dictionary<string, Node>>();

    public static T FindChildByName<T>(this Node node, string name, int maxRecursionDepth = 10, Node initialNode = null) where T : Node
    {
        if (node == null || name == null) return null;

        if (initialNode == null)
        {
            initialNode = node;

            var dict = findChildByNameCache.GetOrCreateValue(node);

            Node ret;
            if (dict.TryGetValue(name, out ret) && ret.IsInstanceValid() && ret.IsInsideTree())
            {
                return (T)ret;
            }
        }

        var c = node.GetChildCount();
        for (int i = 0; i < c; ++i)
        {
            var n = node.GetChild(i);

            if (n.Name == name)
            {
                if (n is T)
                {
                    var dict = findChildByNameCache.GetOrCreateValue(initialNode);
                    dict[name] = (T)n;
                    return (T)n;
                }
                else
                {
                    GD.Print($"Node {name} is of unexpected type {n.GetType()}");
                }
            }
        }

        if (maxRecursionDepth > 0)
        {
            for (int i = 0; i < c; ++i)
            {
                var n = node.GetChild(i);
                var ret = n.FindChildByName<T>(name, maxRecursionDepth - 1, initialNode);
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
        if (type == typeof(long) || type == typeof(System.Int64)) { WriteAll(mem, BitConverter.GetBytes((long)obj)); return; }
        if (type == typeof(ulong)) { WriteAll(mem, BitConverter.GetBytes((ulong)obj)); return; }
        if (type == typeof(short)) { WriteAll(mem, BitConverter.GetBytes((short)obj)); return; }
        if (type == typeof(ushort)) { WriteAll(mem, BitConverter.GetBytes((ushort)obj)); return; }
        if (type == typeof(float))
        {
            //Console.WriteLine($"It is a {type} / {obj.GetType()}");
            SerializationLog($"The single is {(Half)(float)obj}");
            WriteAll(mem, BitConverter.GetBytes((Half)(float)obj));
            return;
        }
        if (type == typeof(double)) { WriteAll(mem, BitConverter.GetBytes((double)obj)); return; }
        if (type == typeof(Half)) { WriteAll(mem, BitConverter.GetBytes((Half)obj)); return; }
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
            byte[] rawBytes = ((string)obj).ToUtf8Buffer();

            WriteAll(mem, BitConverter.GetBytes(rawBytes.Length));
            WriteAll(mem, rawBytes);
            return;
        }
        if (type == typeof(byte))
        {
            WriteAll(mem, new byte[] { (byte)obj });
            return;
        }
        if (type == typeof(sbyte))
        {
            WriteAll(mem, new byte[] { unchecked((byte)(sbyte)obj) });
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
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            WriteAll(mem, new byte[] { (byte)(obj != null ? 1 : 0) });

            if (obj != null)
            {
                WriteAll(mem, ObjToBytes(obj, type.GetGenericArguments()[0]));
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

        SerializationLog($"Seems it's some kind of object {obj.GetType()}");

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
        if (type == typeof(ulong))
        {
            mem.Read(buffer, 0, 8);
            return BitConverter.ToUInt64(buffer, 0);
        }
        if (type == typeof(short))
        {
            mem.Read(buffer, 0, 2);
            return BitConverter.ToInt16(buffer, 0);
        }
        if (type == typeof(ushort))
        {
            mem.Read(buffer, 0, 2);
            return BitConverter.ToUInt16(buffer, 0);
        }
        if (type == typeof(float))
        {
            mem.Read(buffer, 0, 2);
            SerializationLog($"The single is {BitConverter.ToSingle(buffer, 0)}");
            return (float)BitConverter.ToHalf(buffer, 0);
        }
        if (type == typeof(Half))
        {
            mem.Read(buffer, 0, 2);
            return BitConverter.ToHalf(buffer, 0);
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
        if (type == typeof(sbyte))
        {
            mem.Read(buffer, 0, 1);
            return unchecked((sbyte)buffer[0]);
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

            if (len == -1) return null;

            System.Collections.IList ret = (System.Collections.IList)Activator.CreateInstance(type);

            for (int i = 0; i < len; ++i)
            {
                ret.Add(BytesToObj(mem, type.GenericTypeArguments[0]));
            }

            return ret;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            mem.Read(buffer, 0, 1);

            if (buffer[0] == 1)
            {
                SerializationLog($"Creating non-null Nullable with type {type.GetGenericArguments()[0]}");
                return Activator.CreateInstance(type, BytesToObj(mem, type.GetGenericArguments()[0]));
            }
            else
            {
                return Activator.CreateInstance(type);
            }
        }
        if (type.IsArray)
        {
            mem.Read(buffer, 0, 4);
            var len = BitConverter.ToInt32(buffer, 0);

            if (len == -1) return null;

            AT.True(len < 2_000_000_000);
            AT.True(len >= 0);

            System.Collections.IList ret = (System.Collections.IList)Activator.CreateInstance(type, len);

            for (int i = 0; i < len; ++i)
            {
                ret[i] = BytesToObj(mem, type.GetElementType());
            }

            return ret;
        }

        SerializationLog($"Seems it's some kind of object");

        if (!type.IsSealed) GD.Print($"Warning: {type} should be sealed");

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
            GD.Print(txt);
        }
    }

    private static Random _RootRand = new Random();

    [ThreadStatic] private static Random _Rand;

    private static Random Rand
    {
        get
        {
            if (_Rand == null)
            {
                lock (_RootRand)
                {
                    _Rand = new Random(_RootRand.Next());
                }
            }
            return _Rand;
        }
    }

    public static int RandInt(int minInclusive, int maxExclusive)
    {
        return Rand.Next(minInclusive, maxExclusive);
    }

    public static long RandLong(long minInclusive, long maxExclusive)
    {
        return Rand.NextInt64(minInclusive, maxExclusive);
    }

    public static long RandLong()
    {
        return Rand.NextInt64();
    }

    [Obsolete("Use Random() instead")]
    public static float random()
    {
        return (float)Rand.NextDouble();
    }

    public static float Random()
    {
        return (float)Rand.NextDouble();
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

    public static Vector3 GetGlobalLocation(this Node3D node)
    {
        return node.GlobalTransform.Origin;
    }

    public static void SetGlobalLocation(this Node3D node, Vector3 globalLocation)
    {
        var t = node.GlobalTransform;
        t.Origin = globalLocation;
        node.GlobalTransform = t;
    }

    /*public static void CreateRegularTimer(this Node node, string targetMethodName, float interval, Timer.TimerProcessMode mode = Timer.TimerProcessMode.Idle)
    {
        Timer uploadTimer = new Timer();
        uploadTimer.OneShot = false;
        uploadTimer.Autostart = true;
        uploadTimer.Connect("timeout", node, targetMethodName);
        uploadTimer.WaitTime = interval;
        uploadTimer.ProcessMode = mode;
        node.AddChild(uploadTimer);
    }*/

    /**
     * Determines if this object is still valid (aka it has not been disposed)
     * Because Godot uses reference counting, this should always be called on objects that are
     * referenced from other objects
     */
    public static bool IsInstanceValid(this Godot.GodotObject obj)
    {
        return Godot.GodotObject.IsInstanceValid(obj);
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
            if (parent is T typedParent)
            {
                return typedParent;
            }
            node = parent;
        }
    }

    public static T FindParentByName<T>(this Node node, string name)
    {
        while (true)
        {
            var parent = node.GetParentOrNull<Node>();

            if (parent == null) return default(T);
            if (parent is T typedParent && parent.Name == name)
            {
                return typedParent;
            }
            node = parent;
        }
    }

    public static T FindParentByPredicate<T>(this Node node, Func<T, bool> predicate)
    {
        while (true)
        {
            var parent = node.GetParentOrNull<Node>();

            if (parent == null) return default(T);
            if (parent is T typedParent && predicate(typedParent))
            {
                return typedParent;
            }
            node = parent;
        }
    }

    public static void SpawnOneShotParticleSystem2D(string system, Node contextNode, Vector2 location)
    {
        ResourceLoadMonitor.ThreadedInstantiateAsync<GpuParticles2D>(system, contextNode)
            .Then(res => SpawnOneShotParticleSystem2D(res, contextNode, location));
    }

    public static void SpawnOneShotParticleSystem2D(GpuParticles2D particles, Node contextNode, Vector2 location)
    {
        if (particles == null) return;

        contextNode.GetTree().CurrentScene.AddChild(particles);

        particles.GlobalPosition = location;

        particles.OneShot = true;
        particles.Emitting = true;

        var timer = new Timer();
        timer.Autostart = true;
        timer.WaitTime = 5;
        timer.Connect("timeout", new Callable(particles, "queue_free"));
        particles.AddChild(timer);
    }

    public static void SpawnOneShotParticleSystem(PackedScene system, Node contextNode, Vector3 location)
    {
        if (system == null) return;

        var particles = system.Instantiate<GpuParticles3D>();
        contextNode.GetTree().CurrentScene.AddChild(particles);

        particles.SetGlobalLocation(location);

        particles.OneShot = true;
        particles.Emitting = true;

        var timer = new Timer();
        timer.Autostart = true;
        timer.WaitTime = 5;
        timer.Connect("timeout", new Callable(particles, "queue_free"));
        particles.AddChild(timer);
    }

    public static void SpawnOneShotCPUParticleSystem(PackedScene system, Node contextNode, Vector3 location)
    {
        if (system == null) return;

        var particles = system.Instantiate<CpuParticles3D>();
        contextNode.GetTree().CurrentScene.AddChild(particles);

        particles.SetGlobalLocation(location);

        particles.OneShot = true;
        particles.Emitting = true;

        var timer = new Timer();
        timer.Autostart = true;
        timer.WaitTime = 5;
        timer.Connect("timeout", new Callable(particles, "queue_free"));
        particles.AddChild(timer);
    }

    public static void SpawnOneShotSound(string resName, Node contextNode, Vector3 location, float volume = 15f, float pitchMod = 1f)
    {
        var t = AT.TimeLimit();

        ResourceLoadMonitor.StartLoading<AudioStream>(resName, contextNode, (audioStream) =>
        {
            Util.SpawnOneShotSound(audioStream, contextNode, location, volume, pitchMod);
        }, _ => { });

        t.Limit(0.001f);
    }

    public static void SpawnOneShotSound(AudioStream sample, Node contextNode, Vector3 location, float volume = 15f, float pitchMod = 1f)
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

                if (!((AudioStreamPlayer3D)n).Playing)
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
            availExisting.VolumeDb = volume;
            availExisting.AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseSquareDistance;
            availExisting.DopplerTracking = AudioStreamPlayer3D.DopplerTrackingEnum.PhysicsStep;
            availExisting.PitchScale = pitchMod;

            availExisting.Play();
        }
    }

    public static void SpawnOneShotSound(string resName, Node contextNode, Vector2 location, float volume = 15f, float pitchMod = 1f, float falloffRate = 0.5f)
    {
        ResourceLoadMonitor.StartLoading<AudioStream>(resName, contextNode, (audioStream) =>
        {
            Util.SpawnOneShotSound(audioStream, contextNode, location, volume, pitchMod, falloffRate);
        }, _ => { });
    }

    public static void SpawnOneShotSound(AudioStream sample, Node contextNode, Vector2 location, float volume = 15f, float pitchMod = 1f, float falloffRate = 0.5f)
    {
        if (sample == null) return;

        var r = contextNode.GetTree().CurrentScene;
        var c = r.GetChildCount();

        var existingCount = 0;
        AudioStreamPlayer2D availExisting = null;

        for (int i = 0; i < c; ++i)
        {
            var n = r.GetChild(i);
            if (n is AudioStreamPlayer2D)
            {
                existingCount++;

                if (!((AudioStreamPlayer2D)n).Playing)
                {
                    availExisting = (AudioStreamPlayer2D)n;
                    break;
                }
            }
        }

        if (availExisting == null && existingCount < 10)
        {
            availExisting = new AudioStreamPlayer2D();
            contextNode.GetTree().CurrentScene.AddChild(availExisting);
        }

        if (availExisting != null)
        {
            availExisting.GlobalPosition = location;
            availExisting.Stream = sample;
            availExisting.VolumeDb = volume - contextNode.GetTree().Root.GetCamera2D().GlobalPosition.DistanceTo(location) * falloffRate;
            availExisting.PitchScale = pitchMod;

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

                if (!((AudioStreamPlayer)n).Playing)
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

    public static IEnumerable<T> Single<T>(T v)
    {
        yield return v;
    }

    public static void TakeScreenshot(Node ctx)
    {
        var image = ctx.GetViewport().GetTexture().GetImage();

        Task.Run(() =>
        {
            var dir = Godot.DirAccess.Open("user://");
            dir.MakeDir("screenshots");

            image.SavePng($"user://screenshots/{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.png");
        }).HandleError();
    }

    public static int Square(int n)
    {
        return n * n;
    }

    public static float Square(float n)
    {
        return n * n;
    }

    public static IReadOnlyCollection<T> GetEnumValues<T>() where T : Enum
    {
        return (IReadOnlyCollection<T>)Enum.GetValues(typeof(T));
    }

    public static Error Connect(this Node node, string signal, Action @delegate)
    {
        return node.Connect(signal, Callable.From(@delegate));
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> it)
    {
        return it.OrderBy(it2 => GD.Randi());
    }

    public static T[,] ToMultiDimArray<T>(this IEnumerable<T[]> it)
    {
        int dimension = -1;

        foreach (var it2 in it)
        {
            if (dimension == -1)
                dimension = it2.Length;
            else if (dimension != it2.Length)
                throw new Exception();
        }

        var ret = new T[it.Count(), dimension];
        var i = 0;

        foreach (var it2 in it)
        {
            for (var j = 0; j < dimension; ++j)
            {
                ret[i, j] = it2[j];
            }
            ++i;
        }

        return ret;
    }

    public static IEnumerable<T[]> FromMultiDimArray<T>(this T[,] it)
    {
        for (var i = 0; i < it.GetLength(0); ++i)
        {
            var arr = new T[it.GetLength(1)];
            for (var j = 0; j < arr.Length; ++j)
            {
                arr[j] = it[i, j];
            }
            yield return arr;
        }
    }

    public static T ParseEnumString<T>(string str) where T : Enum
    {
        try
        {
            return (T)Enum.Parse(typeof(T), str);
        }
        catch (ArgumentException ex)
        {
            throw new Exception($"Failed to parse {str} into enum of type {typeof(T)}, possible values are {String.Join(",", GetEnumValues<T>())}");
        }
    }

    public static void StartStaggeredPeriodicTimer(Node root, float interval, Action target, bool duringPhysics = false)
    {
        var timer = new Timer();
        root.AddChild(timer);
        timer.OneShot = false;
        timer.Start(Util.Random() * interval);
        timer.WaitTime = interval;
        timer.Timeout += target;
        timer.ProcessCallback = duringPhysics ? Timer.TimerProcessCallback.Physics : Timer.TimerProcessCallback.Idle;
    }

    public static void StartStaggeredPeriodicTimer(Node root, float interval, Action<float> target, bool duringPhysics = false)
    {
        StartStaggeredPeriodicTimer(root, interval, () => target(interval), duringPhysics);
    }

    public static void StartStaggeredPeriodicTimerWith10TicksPerSecond(Node root, float interval, Action<int> target, bool duringPhysics = false)
    {
        var ticks = Mathf.RoundToInt(interval * 10);
        StartStaggeredPeriodicTimer(root, interval, () => target(ticks), duringPhysics);
    }

    public static void StartOneShotTimer(Node root, float time, Action target, bool duringPhysics = false)
    {
        var timer = new Timer();
        root.AddChild(timer);
        timer.OneShot = true;
        timer.Start(time);
        timer.Timeout += target;
        timer.ProcessCallback = duringPhysics ? Timer.TimerProcessCallback.Physics : Timer.TimerProcessCallback.Idle;
    }

    public static string GetFullyQualifiedNodePath(this Node node)
    {
        var ret = "";

        while (node != null)
        {
            ret = $"/{node.Name}{ret}";
            node = node.GetParent();
        }

        return ret;
    }

    public static Vector3 GetBoneWorldPositionByName(this Skeleton3D it, string boneName)
    {
        it.ForceUpdateAllBoneTransforms();
        return it.GlobalTransform * it.GetBoneGlobalPose(it.FindBone(boneName)).Origin;
    }

    public static int? AsInt32OrNull(this Variant variant)
    {
        if (variant.VariantType != Variant.Type.Nil) return variant.AsInt32();
        return null;
    }

    public static void SwitchScenes(Node contextNode, Node newScene)
    {
        GD.Print($"SwitchScenes({contextNode}, {newScene})");
        var tree = contextNode.GetTree();
        var oldScene = tree.CurrentScene;
        oldScene.ProcessMode = Node.ProcessModeEnum.Disabled;
        oldScene.QueueFree();
        tree.Root.RemoveChild(oldScene);
        tree.Root.AddChild(newScene);
        tree.CurrentScene = newScene;
    }

    public static void HandleError<T>(this Task<T> task)
    {
        task.Then(() => { }, res =>
        {
            if (res != null)
            {
                GD.PushError(res);
            }
        });
    }

    public static void HandleError(this Task task)
    {
        task.Then(() => { }, res =>
        {
            if (res != null)
            {
                GD.PushError(res);
            }
        });
    }

    public static void ChangeSceneAndExecuteOnNewScene<T>(string sceneFileName, Node ctx, Action<T> func) where T : Node
    {
        var tree = ctx.GetTree();
        tree.ChangeSceneToFile(sceneFileName);

        Godot.Node.ChildEnteredTreeEventHandler eventHandler = null;
        eventHandler = ch =>
        {
            if (ch is T)
            {
                func((T)ch);
                tree.Root.ChildEnteredTree -= eventHandler;
            }
            else if (tree.CurrentScene == ch)
            {
                GD.PushWarning($"It looks like we changed the scene to a {ch}, when we were expecting a {typeof(T)}");
                tree.Root.ChildEnteredTree -= eventHandler;
            }
        };

        tree.Root.ChildEnteredTree += eventHandler;
    }

    public static float AbsoluteAngleBetween(float angle1, float angle2)
    {
        return Mathf.Abs(AngleBetween(angle1, angle2));
    }

    public static float AngleBetween(float angle1, float angle2)
    {
        return Vector2.Right.Rotated(angle1).AngleTo(Vector2.Right.Rotated(angle2));
    }

    // Switches the game to FullScreen borderless. For speed, we use an usafe technique in dev that is faster
    // but will supposedly crash some GPU drivers.
    public static void SwitchToFullScreenBorderless()
    {
        if (OS.IsDebugBuild())
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);

            var size = DisplayServer.ScreenGetSize();
            var pos = DisplayServer.ScreenGetPosition();
            GD.Print($"SwitchToFullScreenBorderless: size={size}, pos={pos}");

            DisplayServer.WindowSetSize(size);
            DisplayServer.WindowSetPosition(pos);
        }
        else
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
        }
    }

    public static void ScaleMainWindow(float factor)
    {
        var origSize = DisplayServer.WindowGetSize();
        var newSize = new Vector2I(
            Mathf.RoundToInt(origSize.X * factor),
            Mathf.RoundToInt(origSize.Y * factor)
        );
        DisplayServer.WindowSetSize(newSize);
        DisplayServer.WindowSetPosition(DisplayServer.WindowGetPosition() - (newSize - origSize) / 2);
    }
}