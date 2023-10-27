using System;
using System.IO;
using System.Text;

namespace Serilog.Sinks.RawFile;

class FileStreamWriter : IFileWriter
{
    readonly FileStream underlyingStream;
    readonly Stream outputStream;

    long writtenLength;

    FileStreamWriter(FileStream underlyingStream, Stream outputStream)
    {
        this.underlyingStream = underlyingStream;
        this.outputStream = outputStream;
    }

    public void Dispose()
    {
        outputStream.Dispose();
    }

    public void FlushToDisk()
    {
        outputStream.Flush();
        underlyingStream.Flush(true);
    }

    public long Write(ReadOnlySpan<byte> bytes)
    {
        outputStream.Write(bytes);
        writtenLength += bytes.Length;
        return writtenLength;
    }

    public static FileStreamWriter Create(string path, RawFileLifecycleHooks? hooks, Encoding encoding, out long length)
    {
        FileStream underlyingStream;
        Stream outputStream = underlyingStream = System.IO.File.Open(path, new FileStreamOptions
        {
            Access = FileAccess.Write,
            BufferSize = 1,
            Mode = FileMode.OpenOrCreate,
            Options = FileOptions.None,
            Share = FileShare.Read
        });
        outputStream.Seek(0, SeekOrigin.End);

        if (hooks != null)
        {
            outputStream = hooks.OnFileOpened(path, outputStream, encoding) ??
                           throw new InvalidOperationException($"The file lifecycle hook `{nameof(RawFileLifecycleHooks.OnFileOpened)}(...)` returned `null`.");
        }

        length = underlyingStream.Length;
        return new FileStreamWriter(underlyingStream, outputStream);
    }

}
