/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using System;
using System.Runtime.Serialization.Formatters.Binary;
using Godot;

interface HasVersion
{
    int VERSION { get; }
}

static class SaveLoad<T> where T : HasVersion, new()
{
    public static void Save(T inst, string filename = "default")
    {
        var tmpFileName = $"{filename}.tmp";

        var bf = new BinaryFormatter();
        using (var stream = new System.IO.MemoryStream())
        {
            bf.Serialize(stream, new T().VERSION);
            bf.Serialize(stream, inst);
            stream.Flush();
            using (var of = OpenUserFile(tmpFileName, File.ModeFlags.Write))
            {
                of.StoreBuffer(stream.ToArray());
            }
        }

        RenameUserFile(tmpFileName, filename);

        Console.WriteLine("Save successful");
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
                    Console.WriteLine($"Loaded buffer of {buf.Length} size");
                    stream.Write(buf, 0, buf.Length);
                    stream.Position = 0;
                    var version = (int)bf.Deserialize(stream);
                    if (version != new T().VERSION) throw new InvalidOperationException();
                    inst = (T)bf.Deserialize(stream);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Can't deserialize due to: {ex}");
            if (defaultFactory != null)
                inst = defaultFactory();
            else
                inst = new T();
        }

        return inst;
    }

    private static File OpenUserFile(string filename, File.ModeFlags flags)
    {
        var f = new File();
        f.Open($"user://{filename}", flags);
        return f;
    }

    private static bool UserFileExists(string filename)
    {
        var d = new Directory();
        d.Open("user://");
        return d.FileExists(filename);
    }

    private static void RenameUserFile(string src, string dest)
    {
        var d = new Directory();
        d.Open("user://");
        d.Rename(src, dest);
    }
}