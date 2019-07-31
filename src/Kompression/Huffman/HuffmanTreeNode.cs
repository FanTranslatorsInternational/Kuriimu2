using System.Collections.Generic;
using System.Linq;

namespace Kompression.Huffman
{
    class HuffmanTreeNode
    {
        public int Code { get; set; }
        public int Frequency { get; }
        public HuffmanTreeNode[] Children { get; set; }

        public bool IsLeaf => Children == null;

        public HuffmanTreeNode(int frequency)
        {
            Frequency = frequency;
        }

        public IEnumerable<(int, string)> GetHuffCodes(string seed = "") =>
            Children?.SelectMany((child, i) => child.GetHuffCodes(seed + i)) ?? new[] { (Code, seed) };
    }
}
