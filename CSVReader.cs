/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
        if ((Nullable.GetUnderlyingType(destType) != null || destType.IsSubclassOf(typeof(Resource)) || destType == typeof(Resource)) && target.Trim() == "")
            return null;

        if (Nullable.GetUnderlyingType(destType) != null)
        {
            return AutoCoerce(target, Nullable.GetUnderlyingType(destType));
        }

        if (destType.IsArray)
        {
            var ca = target.Split("|")
                .Select(it => it.Trim())
                .Where(it => it.Length > 0)
                .Select(it => AutoCoerce(it, destType.GetElementType()))
                .ToArray();

            var ret = System.Array.CreateInstance(destType.GetElementType(), ca.Count());
            for (var i = 0; i < ca.Length; ++i)
            {
                ret.SetValue(ca[i], i);
            }
            return ret;
        }

        if (destType.IsSubclassOf(typeof(Color)) || destType == typeof(Color))
        {
            if (target.Trim().Length > 0)
                return new Color(new Regex(@"[^A-Za-z0-9]").Replace(target, ""));
            else
                return new Color(1, 1, 1, 1);
        }

        if (destType.IsSubclassOf(typeof(Enum)))
        {
            try
            {
                return Enum.Parse(destType, target);
            }
            catch (ArgumentException)
            {
                throw new Exception($"Failed to parse {target} into enum of type {destType}");
            }
        }

        if ((destType == typeof(int) || destType == typeof(float)) && target == "")
            return 0;

        if (destType == typeof(int))
            return int.Parse(target);

        if (destType == typeof(float))
            return float.Parse(target);

        if (destType == typeof(bool))
            return target.ToLower() == "true" || target == "1";

        if (destType.IsSubclassOf(typeof(Resource)) || destType == typeof(Resource))
            return GD.Load<Resource>(target.Trim());

        if (destType != typeof(string)) throw new Exception($"Unable to convert {target} to {destType}");

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