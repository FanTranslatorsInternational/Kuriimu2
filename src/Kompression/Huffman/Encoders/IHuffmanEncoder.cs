using System;
using System.Collections.Generic;
using System.IO;
using Kompression.Huffman.Support;

namespace Kompression.Huffman.Encoders
{
    public interface IHuffmanEncoder : IDisposable
    {
        void Encode(byte[] input, List<HuffmanTreeNode> labelList, Stream output);
    }
}
