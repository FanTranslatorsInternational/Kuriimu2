using System.IO;
using Kompression.Exceptions;

namespace Kompression.LempelZiv.Decoders
{
    class LzssDecoder:ILzDecoder
    {
        public void Decode(Stream input, Stream output)
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

            new Lz10Decoder().ReadCompressedData(input, output, decompressedSize);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
