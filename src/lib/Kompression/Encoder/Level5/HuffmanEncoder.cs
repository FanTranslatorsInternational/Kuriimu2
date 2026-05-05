using Kompression.Contract.Configuration;
using Kompression.Contract.Encoder;
using Kompression.Contract.Encoder.Huffman;
using Kompression.Contract.Enums.Encoder.Huffman;
using Kompression.Encoder.Headerless;

namespace Kompression.Encoder.Level5
{
    public class HuffmanEncoder(int bitDepth, NibbleOrder nibbleOrder) : IHuffmanEncoder
    {
        private readonly HuffmanHeaderlessEncoder _encoder = new(bitDepth, nibbleOrder);

        public void Configure(IHuffmanEncoderOptionsBuilder huffmanOptions)
        {
            _encoder.Configure(huffmanOptions);
        }

        public void Encode(Stream input, Stream output, IHuffmanTreeBuilder treeBuilder)
        {
            if (input.Length > 0x1FFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var huffmanMode = bitDepth == 4 ? 2 : 3;
            var compressionHeader = new[] {
                (byte)((byte)(input.Length << 3) | huffmanMode),
                (byte)(input.Length >> 5),
                (byte)(input.Length >> 13),
                (byte)(input.Length >> 21) };
            output.Write(compressionHeader, 0, 4);

            _encoder.Encode(input, output, treeBuilder);
        }
    }
}
