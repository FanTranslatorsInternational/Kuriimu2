using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Komponent.IO
{
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
