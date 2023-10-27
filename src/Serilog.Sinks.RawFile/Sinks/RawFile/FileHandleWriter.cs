using System;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace Serilog.Sinks.RawFile;

class FileHandleWriter : IFileWriter
{
    readonly SafeFileHandle? handle;
    readonly string path;

    FileHandleWriter(SafeFileHandle? handle, string path)
    {
        this.handle = handle;
        this.path = path;
    }

    public void Dispose()
    {
        handle?.Dispose();
    }

    public void FlushToDisk()
    {
    }

    public long Write(ReadOnlySpan<byte> bytes)
    {
        var currentHandle = handle ?? OpenHandle(path);
        try
        {
            long length;
            RandomAccess.Write(currentHandle, bytes, length = RandomAccess.GetLength(currentHandle));
            return length + bytes.Length;
        }
        finally
        {
            if (handle is null)
                currentHandle.Dispose();
        }
    }

    public static FileHandleWriter Create(string path, bool keepFileOpen, out long length)
    {
        var handle = OpenHandle(path);

        length = RandomAccess.GetLength(handle);

        if (keepFileOpen)
            return new FileHandleWriter(handle, path);

        handle.Dispose();
        return new FileHandleWriter(null, path);
    }

    static SafeFileHandle OpenHandle(string path) => System.IO.File.OpenHandle(path, FileMode.Append, FileAccess.Write, FileShare.Read);
}
