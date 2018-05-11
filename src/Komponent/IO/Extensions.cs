using System;
using System.Runtime.InteropServices;

namespace Komponent.IO
{
    public static class Extensions
    {
        // Read
        public static unsafe T BytesToStruct<T>(this byte[] buffer, ByteOrder byteOrder = ByteOrder.LittleEndian, int offset = 0)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));

            Tools.AdjustByteOrder(typeof(T), buffer, byteOrder);

            fixed (byte* pBuffer = buffer)
                return Marshal.PtrToStructure<T>((IntPtr)pBuffer + offset);
        }

        public static unsafe T BytesToStruct<T>(this byte[] buffer, int offset) => BytesToStruct<T>(buffer, ByteOrder.LittleEndian, offset);

        // Write
        public static unsafe byte[] StructToBytes<T>(this T item, ByteOrder byteOrder = ByteOrder.LittleEndian)
        {
            var buffer = new byte[Marshal.SizeOf(typeof(T))];

            fixed (byte* pBuffer = buffer)
                Marshal.StructureToPtr(item, (IntPtr)pBuffer, false);

            Tools.AdjustByteOrder(typeof(T), buffer, byteOrder);

            return buffer;
        }
    }
}
