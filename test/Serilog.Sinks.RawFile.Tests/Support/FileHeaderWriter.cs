using System.IO;
using System.Text;

namespace Serilog.Sinks.RawFile.Tests.Support
{
    class FileHeaderWriter : RawFileLifecycleHooks
    {
        public string Header { get; }

        public FileHeaderWriter(string header)
        {
            Header = header;
        }

        public override Stream OnFileOpened(Stream underlyingStream, Encoding encoding)
        {
            if (underlyingStream.Length == 0)
            {
                var writer = new StreamWriter(underlyingStream, encoding);
                writer.WriteLine(Header);
                writer.Flush();
                underlyingStream.Flush();
            }

            return base.OnFileOpened(underlyingStream, encoding);
        }
    }
}
