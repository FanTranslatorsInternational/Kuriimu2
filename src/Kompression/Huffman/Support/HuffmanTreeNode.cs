using System;
using System.Collections.Generic;
using System.Linq;

namespace Kompression.Huffman.Support
{
    /// <summary>
    /// Represents a node in a huffman tree.
    /// </summary>
    public class HuffmanTreeNode
    {
        /// <summary>
        /// Gets or sets the value this node represents.
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the frequency the value.
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// Gets or sets the two children elements.
        /// </summary>
        public HuffmanTreeNode[] Children { get; set; }

        /// <summary>
        /// Indicates whether this node branches to children or contains a value.
        /// </summary>
        public bool IsLeaf => Children == null;

        /// <summary>
        /// Gets the huffman codes for all values under this node.
        /// </summary>
        /// <returns>The huffman codes for all values under this node.</returns>
        public IEnumerable<(int, string)> GetHuffCodes() =>
            GetHuffCodes("");

        /// <summary>
        /// Calculates the depth of the tree from this node onwards.
        /// </summary>
        /// <returns></returns>
        public int GetDepth() => GetDepth(0);

        private IEnumerable<(int, string)> GetHuffCodes(string seed) =>
            Children?.SelectMany((child, i) => child.GetHuffCodes(seed + i)) ?? new[] { (Code, seed) };

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
