/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using System;
using System.Runtime.Serialization.Formatters.Binary;
using Godot;

class HasVersion : Attribute
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
            using (var of = OpenUserFile(tmpFileName, File.ModeFlags.Write))
            {
                of.StoreBuffer(stream.ToArray());
            }
        }

        RenameUserFile(tmpFileName, filename);

        GD.Print("Save successful");
    }

    public static T LoadOrDefault(string filename = "default", Func<T> defaultFactory = null)
    {
        T inst;

        try
        {
            var bf = new BinaryFormatter();
            using (var file = OpenUserFile(filename, File.ModeFlags.Read))
            {
                using (var stream = new System.IO.MemoryStream())
                {
                    var buf = file.GetBuffer((int)file.GetLen());
                    GD.Print($"Loaded buffer of {buf.Length} size");
                    stream.Write(buf, 0, buf.Length);
                    stream.Position = 0;
                    var version = (int)bf.Deserialize(stream);
                    if (version != CurrentVersion) throw new InvalidOperationException();
                    inst = (T)bf.Deserialize(stream);
                }
            }
        }
        catch (Exception ex)
        {
            GD.Print($"Can't deserialize due to: {ex}");
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

    public static File OpenUserFile(string filename, File.ModeFlags flags)
    {
        var f = new File();
        var ret = f.Open($"user://{filename}", flags);
        if (ret == Error.Ok)
            return f;
        else if (ret == Error.FileNotFound)
            throw new System.IO.FileNotFoundException();
        else
            throw new Exception();
    }

    public static bool UserFileExists(string filename)
    {
        var d = new Directory();
        d.Open("user://");
        return d.FileExists(filename);
    }

    public static void RenameUserFile(string src, string dest)
    {
        var d = new Directory();
        d.Open("user://");
        d.Rename(src, dest);
    }

    public static void DeleteUserFile(string file)
    {
        var d = new Directory();
        d.Open("user://");
        d.Remove(file);
    }

    private static int CurrentVersion => (Attribute.GetCustomAttribute(typeof(T), typeof(HasVersion)) as HasVersion)?.VERSION ?? 0;
}