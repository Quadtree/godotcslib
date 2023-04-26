/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using Godot;

public class SaveLoadMetadata
{
    public int Version;
    public string Filename;
}

class XMLSaveLoad<T> : XMLSaveLoadGeneric<T, SaveLoadMetadata> where T : class, new() { }

class XMLSaveLoadGeneric<T, M>
    where T : class, new()
    where M : SaveLoadMetadata, new()
{
    private const string SAVE_DIRECTORY = "user://saves";

    public static void Save(T inst, string filename = "default", M customMetadata = null, IEnumerable<Type> extraKnownTypes = null)
    {
        EnsureDirectoryExists(SAVE_DIRECTORY);

        filename = InputNameToPath(filename);
        var tmpFileName = $"{filename}.tmp";

        using (var stream = new GodotFileStream(tmpFileName, FileAccess.ModeFlags.Write))
        {
            using (var xmlWriter = XmlDictionaryWriter.Create(stream, new XmlWriterSettings
            {
                Indent = true,
                ConformanceLevel = ConformanceLevel.Fragment,
            }))
            {
                if (customMetadata == null) customMetadata = new M();
                customMetadata.Version = CurrentVersion;
                MetadataSerializer.WriteObject(xmlWriter, customMetadata);
                CreateSerializer(extraKnownTypes).WriteObject(xmlWriter, inst);
            }
        }

        RenameUserFile(tmpFileName, filename);

        GD.Print("Save successful");
    }

    static DataContractSerializer MetadataSerializer => new DataContractSerializer(typeof(M));

    static DataContractSerializer CreateSerializer(IEnumerable<Type> extraKnownTypes)
    {
        return new DataContractSerializer(typeof(T),
            new DataContractSerializerSettings
            {
                PreserveObjectReferences = true,
                KnownTypes = CollectKnownTypes(typeof(T)).Concat(extraKnownTypes ?? Array.Empty<Type>()),
                SerializeReadOnlyTypes = true,
            });
    }

    static Type[] CollectKnownTypes(Type root)
    {
        var open = new Type[] { root }.ToList();
        var closed = new List<Type>();

        while (open.Count > 0)
        {
            var next = open[0];
            open.Remove(next);
            closed.Add(next);

            XMLSaveLoadDependsOn atrib = Attribute.GetCustomAttribute(next, typeof(XMLSaveLoadDependsOn)) as XMLSaveLoadDependsOn;
            if (atrib != null)
            {
                if (atrib.Types != null)
                {
                    foreach (var it in atrib.Types)
                    {
                        if (!open.Contains(it) && !closed.Contains(it)) open.Add(it);
                    }
                }
                else
                {
                    GD.PushError($"Type {next} has the [SLDependsOn] attribute but the types are null");
                }
            }
        }

        return closed.ToArray();
    }

    public static Tuple<M, T> LoadWithMetadata(string filename = "default", bool fullLoad = true, IEnumerable<Type> extraKnownTypes = null)
    {
        var originalFilename = filename;
        filename = InputNameToPath(filename);

        using (var stream = new GodotFileStream(filename, FileAccess.ModeFlags.Read))
        {
            using (var xmlReader = XmlDictionaryReader.Create(stream, new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment,
            }))
            {
                var metadata = (M)MetadataSerializer.ReadObject(xmlReader);
                metadata.Filename = originalFilename;
                if (fullLoad && metadata.Version != CurrentVersion) throw new InvalidOperationException($"This file was saved with version {metadata.Version} and you are on version {CurrentVersion}. Migration between these two versions is not currently possible.");
                return Tuple.Create<M, T>(metadata, fullLoad ? (T)CreateSerializer(extraKnownTypes).ReadObject(xmlReader) : (T)null);
            }
        }
    }

    public static T LoadOrDefault(string filename = "default", Func<T> defaultFactory = null, IEnumerable<Type> extraKnownTypes = null)
    {
        T inst;

        try
        {
            inst = LoadWithMetadata(filename, extraKnownTypes: extraKnownTypes).Item2;
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

    private static string InputNameToPath(string filename)
    {
        return $"{SAVE_DIRECTORY}/{filename}.xml";
    }

    public static void EnsureDirectoryExists(string dirname)
    {
        AT.True(dirname?.Length > 0);
        var dir = DirAccess.Open(dirname);
        if (dir == null)
        {
            var rootDir = DirAccess.Open("user://");
            if (rootDir.MakeDirRecursive(dirname) != Error.Ok)
            {
                throw new Exception();
            }
        }
    }

    public static void Rename(string src, string dest)
    {
        RenameUserFile(src, dest);
    }

    public static void Delete(string filename = "default")
    {
        DeleteUserFile(filename);
    }

    public static bool Exists(string filename)
    {
        return UserFileExists(InputNameToPath(filename));
    }

    public static IEnumerable<string> List()
    {
        EnsureDirectoryExists(SAVE_DIRECTORY);
        var dir = DirAccess.Open(SAVE_DIRECTORY);
        dir.IncludeNavigational = false;
        dir.IncludeHidden = false;
        dir.ListDirBegin();

        try
        {
            while (true)
            {
                var nd = dir.GetNext();
                if (nd.Length == 0) break;
                yield return new Regex(@"\.xml$", RegexOptions.IgnoreCase).Replace(nd, "");
            }
        }
        finally
        {
            dir.ListDirEnd();
        }
    }

    public static IEnumerable<M> ListMetadata()
    {
        EnsureDirectoryExists(SAVE_DIRECTORY);
        var dir = DirAccess.Open(SAVE_DIRECTORY);
        dir.IncludeNavigational = false;
        dir.IncludeHidden = false;
        dir.ListDirBegin();

        try
        {
            while (true)
            {
                var nd = dir.GetNext();
                if (nd.Length == 0) break;
                nd = new Regex(@"\.xml$", RegexOptions.IgnoreCase).Replace(nd, "");
                M metadata = null;
                try
                {
                    metadata = LoadWithMetadata(nd, false).Item1;
                }
                catch (Exception ex)
                {
                    GD.Print($"Error Loading Metadata from {nd}: {ex}");
                }
                if (metadata != null) yield return metadata;
            }
        }
        finally
        {
            dir.ListDirEnd();
        }
    }

    private static FileAccess OpenUserFile(string filename, FileAccess.ModeFlags flags)
    {
        return SaveLoad<T>.OpenUserFile(filename, flags);
    }

    private static bool UserFileExists(string filename)
    {
        return SaveLoad<T>.UserFileExists(filename);
    }

    private static void RenameUserFile(string src, string dest)
    {
        SaveLoad<T>.RenameUserFile(src, dest);
    }

    private static void DeleteUserFile(string file)
    {
        SaveLoad<T>.DeleteUserFile(file);
    }

    public static int CurrentVersion => (Attribute.GetCustomAttribute(typeof(T), typeof(HasVersion)) as HasVersion)?.VERSION ?? 0;
}

public class XMLSaveLoadDependsOn : Attribute
{
    public Type[] Types;

    public XMLSaveLoadDependsOn(params Type[] types)
    {
        this.Types = types;
    }
}