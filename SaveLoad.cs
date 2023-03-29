/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Godot;

// This file uses deprecated and insecure serialization routines. Prefer XMLSaveLoad
#pragma warning disable SYSLIB0011

public class HasVersion : Attribute
{
    public HasVersion(int version)
    {
        this.VERSION = version;
    }

    public int VERSION { get; }
}

static class SaveLoad<T> where T : new()
{
    public static void Save(T inst, string filename = "default")
    {
        var tmpFileName = $"{filename}.tmp";

        var bf = new BinaryFormatter();
        using (var stream = new System.IO.MemoryStream())
        {
            bf.Serialize(stream, CurrentVersion);
            bf.Serialize(stream, inst);
            stream.Flush();
            using (var of = OpenUserFile(tmpFileName, FileAccess.ModeFlags.Write))
            {
                of.StoreBuffer(stream.ToArray());
            }
        }

        RenameUserFile(tmpFileName, filename);

        GD.Print("Save successful");
    }

    class CustomBinder : SerializationBinder
    {
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            base.BindToName(serializedType, out assemblyName, out typeName);
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            var possRet = Type.GetType(typeName);
            if (possRet != null) return possRet;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName == assemblyName)
                {
                    return assembly.GetType(typeName);
                }
            }

            throw new Exception();
        }
    }

    public static T LoadOrDefault(string filename = "default", Func<T> defaultFactory = null)
    {
        T inst;

        try
        {
            var bf = new BinaryFormatter();
            bf.Binder = new CustomBinder();
            using (var file = OpenUserFile(filename, FileAccess.ModeFlags.Read))
            {
                using (var stream = new System.IO.MemoryStream())
                {
                    var buf = file.GetBuffer((int)file.GetLength());
                    GD.Print($"Loaded buffer of {buf.Length} size, current assembly is {Assembly.GetExecutingAssembly().FullName}");
                    stream.Write(buf, 0, buf.Length);
                    stream.Position = 0;
                    var version = (int)bf.Deserialize(stream);
                    if (version != CurrentVersion) throw new InvalidOperationException();
                    GD.Print($"Saved version is {version}, looks OK");
                    inst = (T)bf.Deserialize(stream);
                }
            }
        }
        catch (Exception ex)
        {
            GD.Print($"Can't deserialize \"{filename}\" due to: {ex}");
            if (defaultFactory != null)
                inst = defaultFactory();
            else
                inst = new T();
        }

        return inst;
    }

    public static void Rename(string src, string dest)
    {
        RenameUserFile(src, dest);
    }

    public static void Delete(string filename = "default")
    {
        DeleteUserFile(filename);
    }

    public static FileAccess OpenUserFile(string filename, FileAccess.ModeFlags flags)
    {
        var ret = FileAccess.Open($"user://{filename}", flags);
        if (ret != null)
            return ret;
        else if (FileAccess.GetOpenError() == Error.FileNotFound)
            throw new System.IO.FileNotFoundException();
        else
            throw new Exception();
    }

    public static bool UserFileExists(string filename)
    {
        var d = DirAccess.Open("user://");
        return d.FileExists(filename);
    }

    public static void RenameUserFile(string src, string dest)
    {
        var d = DirAccess.Open("user://");
        var status = d.Rename(src, dest);
        if (status != Error.Ok)
        {
            GD.PushError($"Failed to rename {src} to {dest} due to {status}");
        }
    }

    public static void DeleteUserFile(string file)
    {
        var d = DirAccess.Open("user://");
        d.Remove(file);
    }

    private static int CurrentVersion => (Attribute.GetCustomAttribute(typeof(T), typeof(HasVersion)) as HasVersion)?.VERSION ?? 0;
}

#pragma warning restore SYSLIB0011