using System.IO;
using System.Text;

namespace Serilog.Sinks.RawFile.Tests.Support
{
    /// <inheritdoc />
    /// <summary>
    /// Demonstrates the use of <seealso cref="T:Serilog.FileLifecycleHooks" />, by emptying the file before it's written to
    /// </summary>
    public class TruncateFileHook : RawFileLifecycleHooks
    {
        public override Stream OnFileOpened(Stream underlyingStream, Encoding encoding)
        {
            underlyingStream.SetLength(0);
            return base.OnFileOpened(underlyingStream, encoding);
        }
    }
}
