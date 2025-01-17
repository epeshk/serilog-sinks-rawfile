﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using Serilog.Events;
using Serilog.Parsing;
using Xunit.Sdk;

// ReSharper disable UnusedMember.Global

namespace Serilog.Sinks.RawFile.Tests.Support
{
    static class Some
    {
        static int _counter;

        public static int Int()
        {
            return Interlocked.Increment(ref _counter);
        }

        public static decimal Decimal()
        {
            return Int() + 0.123m;
        }

        public static string String(string? tag = null)
        {
            return (tag ?? "") + "__" + Int();
        }

        public static TimeSpan TimeSpan()
        {
            return System.TimeSpan.FromMinutes(Int());
        }

        public static DateTime Instant()
        {
            return new DateTime(2012, 10, 28) + TimeSpan();
        }

        public static DateTimeOffset OffsetInstant()
        {
            return new DateTimeOffset(Instant());
        }

        public static LogEvent LogEvent(string messageTemplate, params object[] propertyValues)
        {
            var log = new LoggerConfiguration().CreateLogger();
#pragma warning disable Serilog004 // Constant MessageTemplate verifier
            if (!log.BindMessageTemplate(messageTemplate, propertyValues, out var template, out var properties))
#pragma warning restore Serilog004 // Constant MessageTemplate verifier
            {
                throw new XunitException("Template could not be bound.");
            }
            return new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, template, properties);
        }

        public static LogEvent LogEvent(DateTimeOffset? timestamp = null, LogEventLevel level = LogEventLevel.Information)
        {
            return new LogEvent(timestamp ?? OffsetInstant(), level,
                null, MessageTemplate(), Enumerable.Empty<LogEventProperty>());
        }

        public static LogEvent InformationEvent(DateTimeOffset? timestamp = null)
        {
            return LogEvent(timestamp);
        }

        public static LogEvent DebugEvent(DateTimeOffset? timestamp = null)
        {
            return LogEvent(timestamp, LogEventLevel.Debug);
        }

        public static LogEventProperty LogEventProperty()
        {
            return new LogEventProperty(String(), new ScalarValue(Int()));
        }

        public static string NonexistentTempFilePath()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        }

        public static string TempFilePath()
        {
            return Path.GetTempFileName();
        }

        public static string TempFolderPath()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static MessageTemplate MessageTemplate()
        {
            return new MessageTemplateParser().Parse(String());
        }
    }
}
