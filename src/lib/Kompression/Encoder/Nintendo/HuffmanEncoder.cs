using Kompression.Contract.Configuration;
using Kompression.Contract.Encoder;
using Kompression.Contract.Encoder.Huffman;
using Kompression.Contract.Enums.Encoder.Huffman;
using Kompression.Encoder.Headerless;

namespace Kompression.Encoder.Nintendo
{
    // TODO: Find configuration way for bit depths and nibble order
    public class HuffmanEncoder(int bitDepth, NibbleOrder nibbleOrder) : IHuffmanEncoder
    {
        private readonly HuffmanHeaderlessEncoder _encoder = new(bitDepth, nibbleOrder);

        public void Configure(IHuffmanEncoderOptionsBuilder huffmanOptions)
        {
            _encoder.Configure(huffmanOptions);
        }

        public void Encode(Stream input, Stream output, IHuffmanTreeBuilder treeBuilder)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new[] { (byte)(0x20 + bitDepth), (byte)input.Length, (byte)(input.Length >> 8 & 0xFF), (byte)(input.Length >> 16 & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            _encoder.Encode(input, output, treeBuilder);
        }
    }
}
