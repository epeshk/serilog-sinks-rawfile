using System;
using System.Buffers;
using System.Collections.Concurrent;

namespace Serilog.Utils
{
    static class ArrayBufferWriterPool
    {
        const int InitialCapacity = 16 * 1024;
        public const int MaxCapacity = 128 * 1024;

        static readonly ConcurrentQueue<ArrayBufferWriter<byte>> Storage = new ConcurrentQueue<ArrayBufferWriter<byte>>();
        [ThreadStatic] static ArrayBufferWriter<byte>? threadLocal;
        public static ArrayBufferWriter<byte> ThreadLocal
        {
            get
            {
                var writer = threadLocal ??= new ArrayBufferWriter<byte>();
                Reset(writer);
                return writer;
            }
        }

        public static ArrayBufferWriter<byte> Rent() => Storage.TryDequeue(out var abw) ? abw : new ArrayBufferWriter<byte>(InitialCapacity);

        public static void Return(ArrayBufferWriter<byte> abw)
        {
            if (abw.Capacity <= MaxCapacity)
                Storage.Enqueue(abw);
        }

        public static void Reset(ArrayBufferWriter<byte> abw)
        {
#if NET8_0_OR_GREATER
            abw.ResetWrittenCount();
#else
            abw.Clear();
#endif
        }
    }
}
