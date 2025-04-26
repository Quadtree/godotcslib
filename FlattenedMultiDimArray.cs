using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Serialization;
using Godot;
using Godot.NativeInterop;

[DataContract(IsReference = true)]
public class FlattenedMultiDimArray<T>
{
    [DataMember(EmitDefaultValue = false)] protected List<int> Dimensions;
    [DataMember(EmitDefaultValue = false)] protected T[] _Data;
    [DataMember(EmitDefaultValue = false)] protected byte[] _DataCompressedRaw;
    [DataMember(EmitDefaultValue = false)] protected uint _DataCompressedRawChecksum;

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
            var theType = typeof(T);

            if (theType.IsEnum)
            {
                theType = theType.GetEnumUnderlyingType();
            }

            if (theType == typeof(float)) return sizeof(float);
            if (theType == typeof(bool)) return sizeof(bool);
            if (theType == typeof(byte)) return sizeof(byte);
            if (theType == typeof(double)) return sizeof(double);
            if (theType == typeof(int)) return sizeof(int);
            if (theType == typeof(long)) return sizeof(long);
            if (theType == typeof(ushort)) return sizeof(ushort);
            if (theType == typeof(short)) return sizeof(short);

            return -1;
        }
    }

    protected virtual T[] Data
    {
        get
        {
            if (_Data == null)
            {
                var byteArray = Decompress(_DataCompressedRaw);

                var byteArrayChecksum = HashBytes(byteArray);
                if (byteArrayChecksum != _DataCompressedRawChecksum)
                    GD.PushError($"Unserialized and uncompressed data hash does not match! Expected: {_DataCompressedRawChecksum} Actual: {byteArrayChecksum}");

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
                _DataCompressedRawChecksum = HashBytes(byteArray);
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
        compressor.Write(decompressedByteArray);
        compressor.Close();

        var ret = compressedStream.ToArray();

        if (OS.IsDebugBuild())
        {
            var decompressionVerification = Decompress(ret);
            if (!Enumerable.SequenceEqual(decompressedByteArray, decompressionVerification)) GD.PushError("Inconsistency on saving!");
        }

        return ret;
    }

    static byte[] Decompress(byte[] compressedByteArray)
    {
        var decompressedStream = new MemoryStream(compressedByteArray);
        var decompressor = new DeflateStream(decompressedStream, CompressionMode.Decompress);
        var ret = new MemoryStream();
        decompressor.CopyTo(ret);

        return ret.ToArray();
    }

    static uint HashBytes(byte[] toHash)
    {
        var hashingContext = new HashingContext();
        hashingContext.Start(HashingContext.HashType.Sha1);
        hashingContext.Update(toHash);
        var hashBytes = hashingContext.Finish();

        return BitConverter.ToUInt32(hashBytes);
    }
}
