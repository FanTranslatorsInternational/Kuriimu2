using System;
using System.IO;
using Kompression.Huffman.Support;

namespace Kompression.Huffman
{
    public interface IHuffmanEncoder : IDisposable
    {
        void Encode(byte[] input, HuffmanTreeNode rootNode, Stream output);
    }
}
