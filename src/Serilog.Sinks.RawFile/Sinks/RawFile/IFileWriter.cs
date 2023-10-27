using System;

namespace Serilog.Sinks.RawFile;

interface IFileWriter : IDisposable, IFlushableFileSink
{
    long Write(ReadOnlySpan<byte> bytes);
}
