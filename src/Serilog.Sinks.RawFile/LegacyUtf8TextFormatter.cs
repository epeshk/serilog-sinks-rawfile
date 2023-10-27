#if UTF8_RENDERING
using System;
using System.Buffers;
using System.Text;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Utf8.Commons;

namespace Serilog;

class LegacyUtf8TextFormatter : IBufferWriterFormatter
{
  OutputFormatter outputFormatter;

  public LegacyUtf8TextFormatter(string messageTemplate, IFormatProvider? formatProvider)
  {
    outputFormatter = new OutputFormatter(messageTemplate, formatProvider);
  }

  public void Format(LogEvent logEvent, IBufferWriter<byte> buffer)
  {
    var writer = new Utf8Writer(buffer);
    outputFormatter.Format(logEvent, ref writer);
    writer.Flush();
  }

  public Encoding Encoding { get; } = Encoding.UTF8;
}
#endif
