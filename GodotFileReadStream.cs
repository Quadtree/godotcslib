using System;
using System.IO;
using Godot;

public class GodotFileStream : Stream
{
    static bool DEBUG_MODE = false;

    Godot.FileAccess File;
    bool _CanWrite = false;
    bool _CanRead = false;
    bool IsClosed = false;

    public GodotFileStream(string path, Godot.FileAccess.ModeFlags flags)
    {
        var rc = Godot.FileAccess.Open(path, flags);
        if (rc == null && Godot.FileAccess.GetOpenError() != Error.Ok)
        {
            if (Godot.FileAccess.GetOpenError() == Error.FileNotFound)
                throw new FileNotFoundException($"Failed to open {path}: {rc}");
            else
                throw new IOException($"Failed to open {path}: {rc}");
        }

        _CanWrite = flags == Godot.FileAccess.ModeFlags.Write || flags == Godot.FileAccess.ModeFlags.WriteRead || flags == Godot.FileAccess.ModeFlags.ReadWrite;
        _CanRead = flags == Godot.FileAccess.ModeFlags.Read || flags == Godot.FileAccess.ModeFlags.WriteRead || flags == Godot.FileAccess.ModeFlags.ReadWrite;
    }

    public override long Position { get => (long)File.GetPosition(); set => throw new NotImplementedException(); }
    public override long Length => (long)File.GetLength();
    public override bool CanWrite => _CanWrite;
    public override bool CanSeek => false;
    public override bool CanRead => _CanRead;

    public override void Flush()
    {
        File.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (Position >= Length)
        {
            if (DEBUG_MODE) GD.Print($"EOF detected");
            return 0;
        }
        var inBuf = File.GetBuffer(Math.Min(count, Length - Position));

        Buffer.BlockCopy(inBuf, 0, buffer, offset, inBuf.Length);

        if (DEBUG_MODE) GD.Print($"Read stream read {inBuf.Length} bytes {Position}/{Length} ({Length - Position})");

        return inBuf.Length;
    }

    public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }
    public override void SetLength(long value) { throw new NotImplementedException(); }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (offset != 0 || count != buffer.Length)
        {
            var oldBuffer = buffer;
            buffer = new byte[count];
            Buffer.BlockCopy(oldBuffer, offset, buffer, 0, count);
        }
        File.StoreBuffer(buffer);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!IsClosed) Close();
    }

    public override void Close()
    {
        IsClosed = true;
        base.Close();
        File.Flush();
        // we don't close the file. it seems this method was removed?
    }
}