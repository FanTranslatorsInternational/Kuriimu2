using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Exceptions;

/* The same as LZ10 just with another compression header */

namespace Kompression.LempelZiv
{
    public static class LZSS
    {
        public static void Decompress(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x53 ||
                compressionHeader[1] != 0x53 ||
                compressionHeader[2] != 0x5A ||
                compressionHeader[3] != 0x4C)   // "SSZL"
                throw new InvalidCompressionException(nameof(LZSS));

            input.Position = 0xC;
            var decompressedSizeBuffer = new byte[4];
            input.Read(decompressedSizeBuffer, 0, 4);
            var decompressedSize = decompressedSizeBuffer[0] | (decompressedSizeBuffer[1] << 8) | (decompressedSizeBuffer[2] << 16) | (decompressedSizeBuffer[3] << 24);

            LZ10.ReadCompressedData(input, output, decompressedSize);
        }

        public static void Compress(Stream input, Stream output)
        {
            var outputStartPos = output.Position;
            output.Position += 0x10;
            LZ10.WriteCompressedData(input, output);

            var outputPos = output.Position;
            output.Position = outputStartPos;
            output.Write(new byte[] { 0x53, 0x53, 0x5A, 0x4C }, 0, 4);
            output.Position += 8;
            var decompressedSizeBuffer = new[]
            {
                (byte)input.Length,
                (byte)((input.Length>>8)&0xFF),
                (byte)((input.Length>>16)&0xFF),
                (byte)((input.Length>>24)&0xFF),
            };
            output.Write(decompressedSizeBuffer, 0, 4);

            output.Position = outputPos;
        }
    }
}
