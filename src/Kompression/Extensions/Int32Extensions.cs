#if NET_CORE_31
using System.Buffers.Binary;
#endif

namespace Kompression.Extensions
{
    // TODO: Remove after switching to net core 3.1
    static class Int32Extensions
    {
        public static byte[] GetArrayLittleEndian(this int value)
        {
#if NET_CORE_31
            var buffer = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            return buffer;
#else
            return new[] { (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24) };
#endif
        }

        public static byte[] GetArrayBigEndian(this int value)
        {
#if NET_CORE_31
            var buffer = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(buffer, value);
            return buffer;
#else
            return new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
#endif
        }
    }
}
