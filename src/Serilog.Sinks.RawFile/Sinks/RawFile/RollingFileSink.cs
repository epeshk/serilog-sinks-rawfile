// Copyright 2013-2023 Serilog Contributors
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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Utils;

namespace Serilog.Sinks.RawFile
{
    sealed class RollingFileSink : ILogEventSink, IFlushableFileSink, IDisposable
    {
        readonly PathRoller _roller;
        readonly IBufferWriterFormatter _textFormatter;
        readonly long? _fileSizeLimitBytes;
        readonly int? _retainedFileCountLimit;
        readonly TimeSpan? _retainedFileTimeLimit;
        readonly bool keepFileOpen;
        readonly bool pauseOnError;
        readonly bool _buffered;
        readonly bool _rollOnFileSizeLimit;
        readonly RawFileLifecycleHooks? _hooks;

        readonly object _syncRoot = new object();
        bool _isDisposed;
        DateTime _nextCheckpoint = DateTime.MinValue;
        FileSink? _currentFile;
        int? _currentFileSequence;

        public RollingFileSink(string path,
                              IBufferWriterFormatter textFormatter,
                              long? fileSizeLimitBytes,
                              int? retainedFileCountLimit,
                              bool buffered,
                              RawFileRollingInterval rollingInterval,
                              bool rollOnFileSizeLimit,
                              RawFileLifecycleHooks? hooks,
                              TimeSpan? retainedFileTimeLimit,
                              bool keepFileOpen,
                              bool pauseOnError)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (fileSizeLimitBytes.HasValue && fileSizeLimitBytes < 1) throw new ArgumentException("Invalid value provided; file size limit must be at least 1 byte, or null.");
            if (retainedFileCountLimit.HasValue && retainedFileCountLimit < 1) throw new ArgumentException("Zero or negative value provided; retained file count limit must be at least 1");
            if (retainedFileTimeLimit.HasValue && retainedFileTimeLimit < TimeSpan.Zero) throw new ArgumentException("Negative value provided; retained file time limit must be non-negative.", nameof(retainedFileTimeLimit));

            _roller = new PathRoller(path, rollingInterval);
            _textFormatter = textFormatter;
            _fileSizeLimitBytes = fileSizeLimitBytes;
            _retainedFileCountLimit = retainedFileCountLimit;
            _retainedFileTimeLimit = retainedFileTimeLimit;
            this.keepFileOpen = keepFileOpen;
            this.pauseOnError = pauseOnError;
            _buffered = buffered;
            _rollOnFileSizeLimit = rollOnFileSizeLimit;
            _hooks = hooks;
        }

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
                if (_isDisposed) ThrowObjectDisposedException();

                var now = logEvent.Timestamp.DateTime;

                if (_currentFile is null || logEvent.Timestamp >= _nextCheckpoint)
                    AlignCurrentFileTo(now);

                while (!EmitOrOverflow(logEvent) && _rollOnFileSizeLimit)
                {
                    AlignCurrentFileTo(now, nextSequence: true);
                }
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
            {
                if (_isDisposed) ThrowObjectDisposedException();

                var now = logEvent.Timestamp.DateTime;

                if (_currentFile is null || logEvent.Timestamp >= _nextCheckpoint)
                    AlignCurrentFileTo(now);

                while (!EmitOrOverflow(writer.WrittenSpan) && _rollOnFileSizeLimit)
                {
                    AlignCurrentFileTo(now, nextSequence: true);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool EmitOrOverflow(LogEvent logEvent)
        {
            var file = _currentFile;
            return file is null || file.EmitOrOverflow(logEvent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool EmitOrOverflow(ReadOnlySpan<byte> renderedLogEvent)
        {
            var file = _currentFile;
            return file is null || file.EmitOrOverflow(renderedLogEvent);
        }

        static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException("The log file has been disposed.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void AlignCurrentFileTo(DateTime now, bool nextSequence = false)
        {
            if (_nextCheckpoint == DateTime.MinValue)
            {
                OpenFile(now);
            }
            else if (nextSequence || now >= _nextCheckpoint)
            {
                int? minSequence = null;
                if (nextSequence)
                {
                    if (_currentFileSequence == null)
                        minSequence = 1;
                    else
                        minSequence = _currentFileSequence.Value + 1;
                }

                CloseFile();
                OpenFile(now, minSequence);
            }
        }

        void OpenFile(DateTime now, int? minSequence = null)
        {
            var currentCheckpoint = _roller.GetCurrentCheckpoint(now);

            // We only try periodically because repeated failures
            // to open log files REALLY slow an app down.
            _nextCheckpoint = _roller.GetNextCheckpoint(now) ?? now.AddMinutes(30);

            var existingFiles = Enumerable.Empty<string>();
            try
            {
                if (Directory.Exists(_roller.LogFileDirectory))
                {
                    existingFiles = Directory.GetFiles(_roller.LogFileDirectory, _roller.DirectorySearchPattern)
                                        .Select(static f => Path.GetFileName(f));
                }
            }
            catch (DirectoryNotFoundException) { }

            var latestForThisCheckpoint = _roller
                .SelectMatches(existingFiles)
                .Where(m => m.DateTime == currentCheckpoint)
                .MaxBy(static m => m.SequenceNumber);

            var sequence = latestForThisCheckpoint?.SequenceNumber;
            if (minSequence != null)
            {
                if (sequence == null || sequence.Value < minSequence.Value)
                    sequence = minSequence;
            }

            const int maxAttempts = 3;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                _roller.GetLogFilePath(now, sequence, out var path);

                try
                {
                    _currentFile = new FileSink(path, _textFormatter, _fileSizeLimitBytes, _buffered, _hooks, keepFileOpen, pauseOnError);

                    _currentFileSequence = sequence;
                }
                catch (IOException ex)
                {
                    if (IOErrors.IsLockedFile(ex))
                    {
                        SelfLog.WriteLine("File target {0} was locked, attempting to open next in sequence (attempt {1})", path, attempt + 1);
                        sequence = (sequence ?? 0) + 1;
                        continue;
                    }

                    throw;
                }

                ApplyRetentionPolicy(path, now);
                return;
            }
        }

        void ApplyRetentionPolicy(string currentFilePath, DateTime now)
        {
            if (_retainedFileCountLimit == null && _retainedFileTimeLimit == null) return;

            var currentFileName = Path.GetFileName(currentFilePath);

            // We consider the current file to exist, even if nothing's been written yet,
            // because files are only opened on response to an event being processed.
            var potentialMatches = Directory.GetFiles(_roller.LogFileDirectory, _roller.DirectorySearchPattern)
                .Select(f => Path.GetFileName(f))
                .Union(new[] { currentFileName });

            var newestFirst = _roller
                .SelectMatches(potentialMatches)
                .OrderByDescending(m => m.DateTime)
                .ThenByDescending(m => m.SequenceNumber);

            var toRemove = newestFirst
                .Where(n => StringComparer.OrdinalIgnoreCase.Compare(currentFileName, n.Filename) != 0)
                .SkipWhile((f, i) => ShouldRetainFile(f, i, now))
                .Select(x => x.Filename)
                .ToList();

            foreach (var obsolete in toRemove)
            {
                var fullPath = Path.Combine(_roller.LogFileDirectory, obsolete);
                try
                {
                    _hooks?.OnFileDeleting(fullPath);
                    System.IO.File.Delete(fullPath);
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Error {0} while processing obsolete log file {1}", ex, fullPath);
                }
            }
        }

        bool ShouldRetainFile(RollingLogFile file, int index, DateTime now)
        {
            if (_retainedFileCountLimit.HasValue && index >= _retainedFileCountLimit.Value - 1)
                return false;

            if (_retainedFileTimeLimit.HasValue && file.DateTime.HasValue &&
                file.DateTime.Value < now.Subtract(_retainedFileTimeLimit.Value))
            {
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_currentFile == null) return;
                CloseFile();
                _isDisposed = true;
            }
        }

        void CloseFile()
        {
            if (_currentFile != null)
            {
                (_currentFile as IDisposable)?.Dispose();
                _currentFile = null;
            }

            _nextCheckpoint = DateTime.MinValue;
        }

        public void FlushToDisk()
        {
            lock (_syncRoot)
            {
                _currentFile?.FlushToDisk();
            }
        }
    }
}
