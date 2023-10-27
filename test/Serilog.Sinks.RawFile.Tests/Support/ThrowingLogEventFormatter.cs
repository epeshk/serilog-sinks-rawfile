using System;
using System.Buffers;
using System.Text;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.RawFile.Tests.Support
{
    public class ThrowingLogEventFormatter : IBufferWriterFormatter
    {
        public void Format(LogEvent logEvent, IBufferWriter<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public Encoding Encoding { get; } = Encoding.UTF8;
    }
}
