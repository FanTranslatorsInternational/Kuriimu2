using Kompression.Huffman.Decoders;
using Kompression.Huffman.Encoders;

namespace Kompression.Huffman
{
    public class Huffman4Le : BaseHuffman
    {
        protected override int BitDepth => 4;
        protected override ByteOrder ByteOrder => ByteOrder.LittleEndian;

        protected override IHuffmanEncoder CreateEncoder()
        {
            return new NintendoHuffmanEncoder(BitDepth);
        }

        protected override IHuffmanDecoder CreateDecoder()
        {
            return new NintendoHuffmanDecoder(BitDepth, ByteOrder);
        }
    }
}
