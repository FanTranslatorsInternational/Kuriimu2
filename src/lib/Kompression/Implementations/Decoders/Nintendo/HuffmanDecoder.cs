using System.IO;
using Kompression.Exceptions;
using Kompression.Implementations.Decoders.Headerless;
using Kontract.Kompression.Interfaces.Configuration;
using Kontract.Models.IO;

namespace Kompression.Implementations.Decoders.Nintendo
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
            if (compressionHeader[0] != 0x20 + _bitDepth)
                throw new InvalidCompressionException($"Nintendo Huffman{_bitDepth}");

            var decompressedLength = compressionHeader[1] | (compressionHeader[2] << 8) | (compressionHeader[3] << 16);

            _decoder.Decode(input, output, decompressedLength);
        }

        public void Dispose()
        {
            // nothing to dispose
        }
    }
}
