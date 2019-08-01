using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.Huffman.Encoders
{
    public interface IHuffmanEncoder : IDisposable
    {
        void Encode(byte[] input, List<HuffmanTreeNode> labelList, Stream output);
    }
}
