using Kompression.Huffman.Support;
using Kompression.IO;

namespace Kompression.Interfaces
{
    // TODO: Documentation
    public interface IHuffmanTreeBuilder
    {
        HuffmanTreeNode Build(byte[] input, int bitDepth, ByteOrder byteOrder);
    }
}
