using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using Godot;

[DataContract(IsReference = true)]
public class FlattenedMultiDimArray<T>
{
    [DataMember] protected List<int> Dimensions;
    [DataMember] protected T[] _Data;
    [DataMember] protected byte[] _DataCompressedRaw;

    public FlattenedMultiDimArray() { }

    public FlattenedMultiDimArray(T[,] arr)
    {
        AT.NotNull(arr);
        Dimensions = new int[] { arr.GetLength(0), arr.GetLength(1) }.ToList();
        var data = new T[arr.GetLength(0) * arr.GetLength(1)];

        int n = 0;

        for (int i = 0; i < Dimensions[0]; ++i)
        {
            for (int j = 0; j < Dimensions[1]; ++j)
            {
                data[n++] = arr[i, j];
            }
        }

        this.Data = data;
    }

    public FlattenedMultiDimArray(T[,,] arr)
    {
        Dimensions = new int[] { arr.GetLength(0), arr.GetLength(1), arr.GetLength(2) }.ToList();
        var data = new T[arr.GetLength(0) * arr.GetLength(1) * arr.GetLength(2)];

        int n = 0;

        for (int i = 0; i < Dimensions[0]; ++i)
        {
            for (int j = 0; j < Dimensions[1]; ++j)
            {
                for (int k = 0; k < Dimensions[2]; ++k)
                {
                    data[n++] = arr[i, j, k];
                }
            }
        }

        this.Data = data;
    }

    int PrimitiveElementSize
    {
        get
        {
            if (typeof(T) == typeof(float)) return sizeof(float);
            if (typeof(T) == typeof(bool)) return sizeof(bool);
            if (typeof(T) == typeof(byte)) return sizeof(byte);
            if (typeof(T) == typeof(double)) return sizeof(double);
            if (typeof(T) == typeof(int)) return sizeof(int);
            if (typeof(T) == typeof(long)) return sizeof(long);
            if (typeof(T) == typeof(ushort)) return sizeof(ushort);
            if (typeof(T) == typeof(short)) return sizeof(short);

            return -1;
        }
    }

    protected virtual T[] Data
    {
        get
        {
            if (PrimitiveElementSize != -1)
            {
                var byteArray = Decompress(_DataCompressedRaw);
                var ret = new T[byteArray.Length / PrimitiveElementSize];
                Buffer.BlockCopy(byteArray, 0, ret, 0, byteArray.Length);
                return ret;
            }
            else return _Data;
        }
        set
        {
            if (PrimitiveElementSize != -1)
            {
                var byteArray = new byte[PrimitiveElementSize * value.Length];
                Buffer.BlockCopy(value, 0, byteArray, 0, byteArray.Length);
                _DataCompressedRaw = Compress(byteArray);
            }
            else _Data = value;
        }
    }

    public T[,] As2D
    {
        get
        {
            if (Dimensions.Count != 2) throw new System.Exception();

            var arr = new T[Dimensions[0], Dimensions[1]];
            int n = 0;
            var data = this.Data;

            for (int i = 0; i < Dimensions[0]; ++i)
            {
                for (int j = 0; j < Dimensions[1]; ++j)
                {
                    arr[i, j] = data[n++];
                }
            }

            return arr;
        }
    }

    public T[,,] As3D
    {
        get
        {
            if (Dimensions.Count != 3) throw new System.Exception();

            var arr = new T[Dimensions[0], Dimensions[1], Dimensions[2]];
            int n = 0;
            var data = this.Data;

            for (int i = 0; i < Dimensions[0]; ++i)
            {
                for (int j = 0; j < Dimensions[1]; ++j)
                {
                    for (int k = 0; k < Dimensions[2]; ++k)
                    {
                        arr[i, j, k] = data[n++];
                    }
                }
            }

            return arr;
        }
    }

    static byte[] Compress(byte[] decompressedByteArray)
    {
        var compressedStream = new MemoryStream();
        var compressor = new DeflateStream(compressedStream, CompressionMode.Compress);
        compressor.Write(decompressedByteArray, 0, decompressedByteArray.Length);
        compressor.Close();

        var ret = compressedStream.ToArray();
        if ((decompressedByteArray.Length > 0 && ret.Length <= 0) || Enumerable.SequenceEqual(decompressedByteArray, ret)) throw new Exception();
        //GD.Print($"{decompressedByteArray.Length} DEFLATE {ret.Length}");
        return ret;
    }

    static byte[] Decompress(byte[] compressedByteArray)
    {
        var decompressedStream = new MemoryStream(compressedByteArray);
        var decompressor = new DeflateStream(decompressedStream, CompressionMode.Decompress);

        var ret = new List<byte[]>();
        int numRead = 0;

        do
        {
            ret.Add(new byte[4096]);
        }
        while ((numRead = decompressor.Read(ret.Last(), 0, ret.Last().Length)) > 0);

        return ret.SelectMany(it => it).ToArray();
    }
}
