using Kompression.Contract.Decoder;
using Kompression.Contract.Enums.Encoder.Huffman;
using Kompression.Decoder.Headerless;
using Kompression.Exceptions;

namespace Kompression.Decoder.Nintendo
{
    public class HuffmanDecoder(int bitDepth, NibbleOrder nibbleOrder) : IDecoder
    {
        private readonly HuffmanHeaderlessDecoder _decoder = new(bitDepth, nibbleOrder);

        public void Decode(Stream input, Stream output)
        {
            var buffer = new byte[4];

            _ = input.Read(buffer);
            if (buffer[0] != 0x20 + bitDepth)
                throw new InvalidCompressionException($"Nintendo Huffman{bitDepth}");

            int decompressedLength = buffer[1] | buffer[2] << 8 | buffer[3] << 16;

            _decoder.Decode(input, output, decompressedLength);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
