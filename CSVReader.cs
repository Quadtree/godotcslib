/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Godot;
using Godot.Collections;

public static class CSVReader
{
    class ColumnDef<T>
    {
        public string Name;
        public Type Type;
        public Action<T, string> Setter;
    }

    private static object AutoCoerce(string target, Type destType)
    {
        if (destType.IsSubclassOf(typeof(Enum)))
            return Enum.Parse(destType, target);

        if (destType.IsArray && destType.GetElementType() == typeof(string))
            return target.Split("|");

        if (destType == typeof(int) && target == "")
            return 0;

        if (destType == typeof(int))
            return int.Parse(target);

        return target;
    }

    public static IList<T> Read<T>(string filename) where T : new()
    {
        var f = FileAccess.Open(filename, FileAccess.ModeFlags.Read);

        var columnDefs = new List<ColumnDef<T>>();

        foreach (var headerName in f.GetCsvLine())
        {
            var cd = new ColumnDef<T>
            {
                Name = headerName,
            };

            var field = typeof(T).GetField(headerName);
            if (field != null)
            {
                cd.Type = field.FieldType;
                cd.Setter = (obj, value) =>
                {
                    field.SetValue(obj, AutoCoerce(value, cd.Type));
                };
            }
            else
            {
                var property = typeof(T).GetProperty(headerName);
                if (property != null)
                {
                    cd.Type = property.PropertyType;
                    cd.Setter = (obj, value) =>
                    {
                        property.SetValue(obj, AutoCoerce(value, cd.Type));
                    };
                }
                else
                {
                    throw new Exception($"No field or property named {headerName} on {typeof(T)}");
                }
            }

            if (cd.Setter == null) throw new Exception();
            if (cd.Type == null) throw new Exception();
            columnDefs.Add(cd);
        }

        var ret = new List<T>();

        while (f.GetPosition() < f.GetLength())
        {
            var values = f.GetCsvLine();
            var retRow = new T();

            for (int i = 0; i < columnDefs.Count; ++i)
            {
                var columnDef = columnDefs[i];
                var value = i < values.Length ? values[i] : "";

                columnDef.Setter(retRow, value);
            }

            ret.Add(retRow);
        }

        return ret;
    }
}