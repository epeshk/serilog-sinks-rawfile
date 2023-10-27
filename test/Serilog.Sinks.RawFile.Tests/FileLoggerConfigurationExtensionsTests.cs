using System;
using System.IO;
using System.Threading;
using Serilog.Sinks.RawFile.Tests.Support;
using Xunit;

namespace Serilog.Sinks.RawFile.Tests
{
    public class FileLoggerConfigurationExtensionsTests
    {
        static readonly string InvalidPath = new string(Path.GetInvalidPathChars());

        [Fact]
        public void WhenWritingCreationExceptionsAreSuppressed()
        {
            new LoggerConfiguration()
                .WriteTo.RawFile(InvalidPath)
                .CreateLogger();
        }

        [Fact]
        public void WhenAuditingCreationExceptionsPropagate()
        {
            Assert.Throws<ArgumentException>(() =>
                new LoggerConfiguration()
                    .AuditTo.RawFile(InvalidPath)
                    .CreateLogger());
        }

        [Fact]
        public void WhenWritingLoggingExceptionsAreSuppressed()
        {
            using (var tmp = TempFolder.ForCaller())
            using (var log = new LoggerConfiguration()
                .WriteTo.RawFile(new ThrowingLogEventFormatter(), tmp.AllocateFilename())
                .CreateLogger())
            {
                log.Information("Hello");
            }
        }

        [Fact]
        public void WhenAuditingLoggingExceptionsPropagate()
        {
            using (var tmp = TempFolder.ForCaller())
            using (var log = new LoggerConfiguration()
                .AuditTo.RawFile(new ThrowingLogEventFormatter(), tmp.AllocateFilename())
                .CreateLogger())
            {
                var ex = Assert.Throws<AggregateException>(() => log.Information("Hello"));
                Assert.IsType<NotImplementedException>(ex.GetBaseException());
            }
        }

        [Fact]
        public void WhenFlushingToDiskReportedFileSinkCanBeCreatedAndDisposed()
        {
            using (var tmp = TempFolder.ForCaller())
            using (var log = new LoggerConfiguration()
                .WriteTo.RawFile(tmp.AllocateFilename(), flushToDiskInterval: TimeSpan.FromMilliseconds(500))
                .CreateLogger())
            {
                log.Information("Hello");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }
    }
}
