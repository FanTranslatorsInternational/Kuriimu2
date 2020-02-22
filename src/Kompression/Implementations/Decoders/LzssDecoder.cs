using System.IO;
using Kompression.Exceptions;
using Kontract.Kompression.Configuration;

namespace Kompression.Implementations.Decoders
{
    public class LzssDecoder : IDecoder
    {
        private Lz10Decoder _lz10Decoder;

        public LzssDecoder()
        {
            _lz10Decoder = new Lz10Decoder();
        }

        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);
            if (compressionHeader[0] != 0x53 ||
                compressionHeader[1] != 0x53 ||
                compressionHeader[2] != 0x5A ||
                compressionHeader[3] != 0x4C)   // "SSZL"
                throw new InvalidCompressionException("Lzss");

            input.Position = 0xC;
            var decompressedSizeBuffer = new byte[4];
            input.Read(decompressedSizeBuffer, 0, 4);
            var decompressedSize = decompressedSizeBuffer[0] | (decompressedSizeBuffer[1] << 8) | (decompressedSizeBuffer[2] << 16) | (decompressedSizeBuffer[3] << 24);

            _lz10Decoder.ReadCompressedData(input, output, decompressedSize);
        }

        public void Dispose()
        {
            _lz10Decoder?.Dispose();
            _lz10Decoder = null;
        }
    }
}
