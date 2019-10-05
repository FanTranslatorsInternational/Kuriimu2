using System;

namespace Kompression.Extensions
{
    static class ByteArrayExtensions
    {
        public static int GetInt32LittleEndian(this byte[] data, int position)
        {
            if (position + 3 >= data.Length)
                throw new ArgumentOutOfRangeException(nameof(position));

            return (data[position + 3] << 24) | (data[position + 2] << 16) | (data[position + 1] << 8) | data[position];
        }

        public static int GetInt32BigEndian(this byte[] data, int position)
        {
            if (position + 3 >= data.Length)
                throw new ArgumentOutOfRangeException(nameof(position));

            return (data[position] << 24) | (data[position + 1] << 16) | (data[position + 2] << 8) | data[position + 3];
        }
    }
}
