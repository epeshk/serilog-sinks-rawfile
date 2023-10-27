using System.IO;
using System.IO.Compression;
using System.Text;

namespace Serilog.Sinks.RawFile.Tests.Support
{
    /// <inheritdoc />
    /// <summary>
    /// Demonstrates the use of <seealso cref="T:Serilog.FileLifecycleHooks" />, by compressing log output using GZip
    /// </summary>
    public class GZipHooks : RawFileLifecycleHooks
    {
        readonly int _bufferSize;

        public GZipHooks(int bufferSize = 1024 * 32)
        {
            _bufferSize = bufferSize;
        }

        public override Stream OnFileOpened(Stream underlyingStream, Encoding encoding)
        {
            var compressStream = new GZipStream(underlyingStream, CompressionMode.Compress);
            return new BufferedStream(compressStream, _bufferSize);
        }
    }
}
