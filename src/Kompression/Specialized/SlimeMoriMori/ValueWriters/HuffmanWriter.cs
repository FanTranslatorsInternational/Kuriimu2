using System.Collections.Generic;
using System.Linq;
using Komponent.IO;
using Kontract.Kompression.Model.Huffman;

namespace Kompression.Specialized.SlimeMoriMori.ValueWriters
{
    class HuffmanWriter : IValueWriter
    {
        private IDictionary<int, string> _huffmanCodes;

        public HuffmanWriter(HuffmanTreeNode huffmanTree)
        {
            _huffmanCodes = huffmanTree.GetHuffCodes().ToDictionary(node => node.Item1, node => node.Item2);
        }

        public void WriteValue(BitWriter bw, byte value)
        {
            var code = _huffmanCodes[value].Select(x => x - '0').Aggregate((a, b) => (a << 1) | b);
            bw.WriteBits(code, _huffmanCodes[value].Length);
        }
    }
}
