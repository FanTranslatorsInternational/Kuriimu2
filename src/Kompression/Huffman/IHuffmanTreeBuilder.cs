using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Huffman.Support;

namespace Kompression.Huffman
{
    public interface IHuffmanTreeBuilder
    {
        HuffmanTreeNode Build(byte[] input, int bitDepth, ByteOrder byteOrder);
    }
}
