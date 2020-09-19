using System;
#if NET_CORE_31
using System.Buffers.Binary;
#endif

namespace Kompression.Extensions
{
    // TODO: Remove after switching to net core 3.1
    static class ByteArrayExtensions
    {
        public static int GetInt32LittleEndian(this byte[] data, int position)
        {
#if NET_CORE_31
            return BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(position));
#else
            if (position + 3 >= data.Length)
                throw new ArgumentOutOfRangeException(nameof(position));

            return (data[position + 3] << 24) | (data[position + 2] << 16) | (data[position + 1] << 8) | data[position];
#endif
        }

        public static int GetInt32BigEndian(this byte[] data, int position)
        {
#if NET_CORE_31
            return BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(position));
#else
            if (position + 3 >= data.Length)
                throw new ArgumentOutOfRangeException(nameof(position));

            return (data[position] << 24) | (data[position + 1] << 16) | (data[position + 2] << 8) | data[position + 3];
#endif
        }
    }
}
