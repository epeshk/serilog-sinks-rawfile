using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.RawFile.Tests.Support;
using Xunit;

namespace Serilog.Sinks.RawFile.Tests;

public class ThreadSafetyTests
{
    [Fact]
    public void FileSink_ShouldBeThreadSafe()
    {
        using var tmp = TempFolder.ForCaller();
        var path = tmp.AllocateFilename("txt");
        var evt = Some.LogEvent("Hello, world!");

        const int N = 256 * 1024;

        using (var sink = new FileSink(path, new CharByCharFormatter(), null, false, null, true, false))
        {
            var countdownEvent = new CountdownEvent(2);
            var t1 = new Thread(() => WriteToSink(sink, evt, N, countdownEvent));
            var t2 = new Thread(() => WriteToSink(sink, evt, N, countdownEvent));
            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();
        }

        var lines = System.IO.File.ReadAllLines(path);

        Assert.Equal(2 * N, lines.Length);
        Assert.Single(lines.Distinct());
    }

    [Fact]
    public void RollingFileSink_ShouldBeThreadSafe()
    {
        using var tmp = TempFolder.ForCaller();
        var path = tmp.AllocateFilename("txt");
        var evt = Some.LogEvent("Hello, world!");

        const int N = 1024 * 1024;

        using (var sink = new RollingFileSink(path, new CharByCharFormatter(), null, 31, true, RawFileRollingInterval.Infinite, false, null, null, true, false))
        {
            var countdownEvent = new CountdownEvent(2);
            var t1 = new Thread(() => WriteToSink(sink, evt, N, countdownEvent));
            var t2 = new Thread(() => WriteToSink(sink, evt, N, countdownEvent));
            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();
        }

        var lines = System.IO.File.ReadAllLines(path);

        Assert.Equal(2 * N, lines.Length);
        Assert.Single(lines.Distinct());
    }

    static void WriteToSink(ILogEventSink sink, LogEvent logEvent, int count, CountdownEvent countdownEvent)
    {
        countdownEvent.Signal();
        countdownEvent.Wait();

        while (count --> 0)
            sink.Emit(logEvent);
    }
}

class CharByCharFormatter : IBufferWriterFormatter
{
    public void Format(LogEvent logEvent, IBufferWriter<byte> buffer)
    {
        foreach (var c in logEvent.MessageTemplate.Text)
        {
            var span = buffer.GetSpan(1);
            span[0] = (byte)c;
            buffer.Advance(1);
        }

        var span1 = buffer.GetSpan(2);
        span1[0] = (byte)'\r';
        span1[1] = (byte)'\n';
        buffer.Advance(2);
    }

    public Encoding Encoding { get; } = Encoding.UTF8;
}
