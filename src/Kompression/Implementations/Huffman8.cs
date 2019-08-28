using Kompression.Huffman;
using Kompression.Implementations.Decoders;
using Kompression.Implementations.Encoders;

namespace Kompression.Implementations
{
    public class Huffman8 : BaseHuffman
    {
        protected override int BitDepth => 8;

        // Not used here but needs to be set
        protected override ByteOrder ByteOrder => ByteOrder.LittleEndian;

        protected override IHuffmanEncoder CreateEncoder()
        {
            return new NintendoHuffmanEncoder(BitDepth);
        }

        protected override IHuffmanDecoder CreateDecoder()
        {
            return new NintendoHuffmanDecoder(BitDepth, ByteOrder);
        }

        public override string[] Names => new[] { "Nintendo Huffman 8Bit" };
    }
}
