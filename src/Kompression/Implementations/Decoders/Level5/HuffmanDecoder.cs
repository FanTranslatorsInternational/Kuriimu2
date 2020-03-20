using System.IO;
using Kompression.Exceptions;
using Kompression.Implementations.Decoders.Headerless;
using Kontract.Kompression.Configuration;
using Kontract.Models.IO;

namespace Kompression.Implementations.Decoders.Level5
{
    public class HuffmanDecoder : IDecoder
    {
        private readonly int _bitDepth;
        private readonly HuffmanHeaderlessDecoder _decoder;

        public HuffmanDecoder(int bitDepth, NibbleOrder nibbleOrder)
        {
            _bitDepth = bitDepth;

            _decoder = new HuffmanHeaderlessDecoder(bitDepth, nibbleOrder);
        }

        public void Decode(Stream input, Stream output)
        {
            var compressionHeader = new byte[4];
            input.Read(compressionHeader, 0, 4);

            var huffmanMode = _bitDepth == 4 ? 2 : 3;
            if ((compressionHeader[0] & 0x7) != huffmanMode)
                throw new InvalidCompressionException($"Level5 Huffman{_bitDepth}");

            var decompressedSize = (compressionHeader[0] >> 3) | (compressionHeader[1] << 5) |
                                   (compressionHeader[2] << 13) | (compressionHeader[3] << 21);

            _decoder.Decode(input, output, decompressedSize);
        }

        public void Dispose()
        {
            // nothing to dispose
        }
    }
}
