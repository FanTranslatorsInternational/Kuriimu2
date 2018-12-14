using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Komponent.IO
{
    public static class Extensions
    {
        //public const byte DecimalSignBit = 128;

        //// Read
        //public static unsafe T BytesToStruct<T>(this byte[] buffer, ByteOrder byteOrder = ByteOrder.LittleEndian, int offset = 0)
        //{
        //    if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));

        //    Tools.AdjustByteOrder(typeof(T), buffer, byteOrder);

        //    fixed (byte* pBuffer = buffer)
        //        return Marshal.PtrToStructure<T>((IntPtr)pBuffer + offset);
        //}

        //public static unsafe T BytesToStruct<T>(this byte[] buffer, int offset) => BytesToStruct<T>(buffer, ByteOrder.LittleEndian, offset);

        //// Write
        //public static unsafe byte[] StructToBytes<T>(this T item, ByteOrder byteOrder = ByteOrder.LittleEndian)
        //{
        //    var buffer = new byte[Marshal.SizeOf(typeof(T))];

        //    fixed (byte* pBuffer = buffer)
        //        Marshal.StructureToPtr(item, (IntPtr)pBuffer, false);

        //    Tools.AdjustByteOrder(typeof(T), buffer, byteOrder);

        //    return buffer;
        //}
    }

    internal class DecimalExtensions
    {
        public static byte[] GetBytes(decimal value)
        {
            var bits = decimal.GetBits(value);
            var bytes = new List<byte>();

            foreach (var i in bits)
                bytes.AddRange(BitConverter.GetBytes(i));

            return bytes.ToArray();
        }

        public static decimal ToDecimal(byte[] value)
        {
            if (value.Length != 16)
                throw new Exception("A decimal must be created from exactly 16 bytes");

            var bits = new int[4];
            for (var i = 0; i <= 15; i += 4)
                bits[i / 4] = BitConverter.ToInt32(value, i);

            return new decimal(bits);
        }
    }
}
