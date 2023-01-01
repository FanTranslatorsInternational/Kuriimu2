﻿using Kontract.Kompression.Models.Huffman;
using Kontract.Models.IO;

namespace Kontract.Kompression.Interfaces
{
    /// <summary>
    /// Provides functionality to create a huffman tree.
    /// </summary>
    public interface IHuffmanTreeBuilder
    {
        /// <summary>
        /// Builds a huffman tree encoding.
        /// </summary>
        /// <param name="input">The input data to convert to a tree.</param>
        /// <param name="bitDepth">The bit depth of a unit to encode.</param>
        /// <param name="nibbleOrder">The order of nibbles to order the values in.</param>
        /// <returns>The root node of the created tree.</returns>
        HuffmanTreeNode Build(byte[] input, int bitDepth, NibbleOrder nibbleOrder);
    }
}
