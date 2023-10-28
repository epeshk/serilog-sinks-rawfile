// Copyright 2013-2016 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Utils;

namespace Serilog.Sinks.RawFile
{
    /// <summary>
    /// Write log events to a disk file.
    /// </summary>
    sealed class FileSink : IFileSink, IDisposable
    {
        const int MaxInMemoryCapacity = 64 * 1024 * 1024;

        IFileWriter file;
        readonly IBufferWriterFormatter _textFormatter;
        readonly long? _fileSizeLimitBytes;
        readonly bool _buffered;
        readonly bool _pauseLoggingOnExceptions;
        readonly object _syncRoot = new object();
        long _bytesWritten;
        Stopwatch? _quarantineStopwatch;

        ArrayBufferWriter<byte> _bufferWriter = ArrayBufferWriterPool.Rent();

        // This overload should be used internally; the overload above maintains compatibility with the earlier public API.
        internal FileSink(
            string path,
            IBufferWriterFormatter textFormatter,
            long? fileSizeLimitBytes,
            bool buffered,
            RawFileLifecycleHooks? hooks,
            bool keepFileOpen,
            bool pauseLoggingOnExceptions)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (fileSizeLimitBytes.HasValue && fileSizeLimitBytes < 1) throw new ArgumentException("Invalid value provided; file size limit must be at least 1 byte, or null.");
            _textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
            _fileSizeLimitBytes = fileSizeLimitBytes;
            _buffered = buffered;
            _pauseLoggingOnExceptions = pauseLoggingOnExceptions;

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            file = hooks is null
                ? FileHandleWriter.Create(path, keepFileOpen, out _bytesWritten)
                : FileStreamWriter.Create(path, hooks, textFormatter.Encoding, out _bytesWritten);
        }

        public bool EmitOrOverflow(LogEvent logEvent)
        {
            var limit = _fileSizeLimitBytes ?? long.MaxValue;
            if (_bytesWritten >= limit)
                return false;

            var writer = _bufferWriter;
            _textFormatter.Format(logEvent, writer);
            if (!_buffered || writer.WrittenCount >= writer.Capacity / 2 || (_fileSizeLimitBytes.HasValue && _bytesWritten + writer.WrittenCount >= limit))
                Flush();

            return true;
        }

        public bool EmitOrOverflow(ReadOnlySpan<byte> renderedLogEvent)
        {
            var limit = _fileSizeLimitBytes ?? long.MaxValue;
            if (_bytesWritten >= limit)
                return false;

            var writer = _bufferWriter;
            writer.Write(renderedLogEvent);
            if (!_buffered || writer.WrittenCount >= writer.Capacity / 2 || (_fileSizeLimitBytes.HasValue && _bytesWritten + writer.WrittenCount >= limit))
                Flush();

            return true;
        }

        void Flush()
        {
            if (_quarantineStopwatch is not null && _quarantineStopwatch.Elapsed < TimeSpan.FromSeconds(5))
            {
                if (_bufferWriter.WrittenCount >= MaxInMemoryCapacity)
                    _bufferWriter.ResetWrittenCount();
                return;
            }

            try
            {
                _bytesWritten = file.Write(_bufferWriter.WrittenSpan);
                _bufferWriter.ResetWrittenCount();
            }
            catch (IOException)
            {
                if (_pauseLoggingOnExceptions)
                    _quarantineStopwatch = Stopwatch.StartNew();
                throw;
            }

            if (_bufferWriter.Capacity > ArrayBufferWriterPool.MaxCapacity)
                _bufferWriter = ArrayBufferWriterPool.Rent();
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="logEvent"/> is <code>null</code></exception>
        public void Emit(LogEvent logEvent)
        {
            ArgumentNullException.ThrowIfNull(logEvent);
            if (!Monitor.TryEnter(_syncRoot))
            {
                PrerenderAndEmit(logEvent);
                return;
            }

            try
            {
                EmitOrOverflow(logEvent);
            }
            finally
            {
                Monitor.Exit(_syncRoot);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void PrerenderAndEmit(LogEvent logEvent)
        {
            var writer = ArrayBufferWriterPool.ThreadLocal;
            _textFormatter.Format(logEvent, writer);

            lock (_syncRoot)
                EmitOrOverflow(writer.WrittenSpan);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_syncRoot)
            {
                FlushToDisk();
                file.Dispose();

                var writer = _bufferWriter;
                _bufferWriter = null!;
                ArrayBufferWriterPool.Return(writer);
            }
        }

        /// <inheritdoc />
        public void FlushToDisk()
        {
            lock (_syncRoot)
            {
                if (_bufferWriter.WrittenCount > 0)
                {
                    Flush();
                }

                file.FlushToDisk();
            }
        }
    }
}
