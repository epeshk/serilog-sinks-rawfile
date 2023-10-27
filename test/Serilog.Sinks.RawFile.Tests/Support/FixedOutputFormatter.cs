using System.Buffers;
using System.Text;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.RawFile.Tests.Support
{
    public class FixedOutputFormatter : IBufferWriterFormatter
    {
        string _substitutionText;

        public FixedOutputFormatter(string substitutionText)
        {
            _substitutionText = substitutionText;
        }

        public void Format(LogEvent logEvent, IBufferWriter<byte> buffer)
        {
            buffer.Write(Encoding.UTF8.GetBytes(_substitutionText));
        }

        public Encoding Encoding { get; } = Encoding.UTF8;
    }
}
