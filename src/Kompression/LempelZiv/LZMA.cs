using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.LempelZiv.Models;

namespace Kompression.LempelZiv
{
    public static class LZMA
    {
        private const int LzmaMinDict = 0x1000;

        public static void Decompress(Stream input, Stream output)
        {
            var properties = (byte)input.ReadByte();
            var dictSize = ReadUInt32(input);
            var decompressedSize = ReadUInt64(input);

            bool useEndMarker = decompressedSize == ulong.MaxValue;
        }

        private static LzmaProperties DecodeProperties(byte properties)
        {
            if (properties > 0xE0)
                throw new NotSupportedException("LZMA Properties not valid.");

            var lc = properties % 9;
            properties /= 9;
            var pb = properties / 5;
            var lp = properties % 5;

            return new LzmaProperties(lc, pb, lp);
        }

        private static uint ReadUInt32(Stream input)
        {
            var result = 0U;
            result |= (uint)input.ReadByte();
            result |= (uint)input.ReadByte() << 8;
            result |= (uint)input.ReadByte() << 16;
            result |= (uint)input.ReadByte() << 24;
            return result;
        }

        private static ulong ReadUInt64(Stream input)
        {
            var result = 0UL;
            result |= (ulong)input.ReadByte();
            result |= (ulong)input.ReadByte() << 8;
            result |= (ulong)input.ReadByte() << 16;
            result |= (ulong)input.ReadByte() << 24;
            result |= (ulong)input.ReadByte() << 32;
            result |= (ulong)input.ReadByte() << 40;
            result |= (ulong)input.ReadByte() << 48;
            result |= (ulong)input.ReadByte() << 56;
            return result;
        }
    }
}
