using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Exceptions;
using Kompression.LempelZiv.Matcher;

namespace Kompression.LempelZiv
{
    public static class LZ60
    {
        public static void Decompress(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x60)   // 0x60 for LZ60, which seems to be the same as LZ40
                throw new InvalidCompressionException(nameof(LZ60));

            var decompressedSize = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            LZ40.ReadCompressedData(input, output, decompressedSize);
        }

        public static void Compress(Stream input, Stream output)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var lzFinder = new NaiveMatcher(3, 0x10010F, 0xFFF, 0);
            var lzResults = lzFinder.FindMatches(input);

            var compressionHeader = new byte[] { 0x60, (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            LZ40.WriteCompressedData(input, output, lzResults);
        }
    }
}
