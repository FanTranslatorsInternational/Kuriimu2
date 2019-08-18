using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kompression.Huffman.Support
{
    public class HuffmanTreeNode
    {
        public int Code { get; set; }
        public int Frequency { get; set; }
        public HuffmanTreeNode[] Children { get; set; }

        public bool IsLeaf => Children == null;

        public HuffmanTreeNode(int frequency)
        {
            Frequency = frequency;
        }

        public IEnumerable<(int, string)> GetHuffCodes() =>
            GetHuffCodes("");

        private IEnumerable<(int, string)> GetHuffCodes(string seed) =>
            Children?.SelectMany((child, i) => child.GetHuffCodes(seed + i)) ?? new[] { (Code, seed) };

        public int GetDepth() => GetDepth(0);

        private int GetDepth(int seed)
        {
            if (IsLeaf || Children == null)
                return seed;

            var depth1 = Children[0].GetDepth(seed + 1);
            var depth2 = Children[1].GetDepth(seed + 1);
            return Math.Max(depth1, depth2);
        }
    }
}
