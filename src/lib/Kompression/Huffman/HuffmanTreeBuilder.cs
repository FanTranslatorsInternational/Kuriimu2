using System.Collections.Generic;
using System.Linq;
using Kompression.Exceptions;
using Kontract.Kompression.Interfaces;
using Kontract.Kompression.Models.Huffman;
using Kontract.Models.IO;

namespace Kompression.Huffman
{
    /// <summary>
    /// Creates a huffman tree and sorts the nodes by frequency.
    /// </summary>
    public class HuffmanTreeBuilder : IHuffmanTreeBuilder
    {
        public HuffmanTreeNode Build(byte[] input, int bitDepth, NibbleOrder nibbleOrder)
        {
            // Get value frequencies of input
            var frequencies = GetFrequencies(input, bitDepth, nibbleOrder).ToList();

            // Add a stub entry in the special case that there's only one item;
            // We want at least 2 elements in the tree to encode
            if (frequencies.Count == 1)
                frequencies.Add(new HuffmanTreeNode
                {
                    Frequency = 0,
                    Code = input[0] + 1
                });

            // Create and sort the tree of frequencies
            var rootNode = CreateAndSortTree(frequencies);

            return rootNode;
        }

        private IEnumerable<HuffmanTreeNode> GetFrequencies(byte[] input, int bitDepth, NibbleOrder? byteOrder)
        {
            if (bitDepth != 4 && bitDepth != 8)
                throw new BitDepthNotSupportedException(bitDepth);

            // Split input into elements of bitDepth size
            IEnumerable<byte> data = input;
            if (bitDepth == 4) data = byteOrder == NibbleOrder.LowNibbleFirst ? 
                input.SelectMany(b => new[] { (byte)(b % 16), (byte)(b / 16) }) : 
                input.SelectMany(b => new[] { (byte)(b / 16), (byte)(b % 16) });

            // Group elements by value
            var groupedElements = data.GroupBy(b => b);

            // Create huffman nodes out of value groups
            return groupedElements.Select(group => new HuffmanTreeNode
            {
                Frequency = group.Count(),
                Code = group.Key
            });
        }

        private HuffmanTreeNode CreateAndSortTree(List<HuffmanTreeNode> frequencies)
        {
            //Sort and create the tree
            while (frequencies.Count > 1)
            {
                // Order frequencies ascending
                frequencies = frequencies.OrderBy(n => n.Frequency).ToList();

                // Create new tree node with the 2 elements of least frequency
                var leastFrequencyNode = new HuffmanTreeNode
                {
                    Frequency = frequencies[0].Frequency + frequencies[1].Frequency,
                    Children = frequencies.Take(2).ToArray()
                };

                // Remove those least frequency elements and append new tree node to frequencies
                frequencies = frequencies.Skip(2).Concat(new[] { leastFrequencyNode }).ToList();

                // This ultimately results in a tree like structure where the most frequent elements are closer to the root;
                // while less frequent elements are farther from it

                // Example:
                // (F:4)
                //   (F:3)
                //     (F:1)
                //     (F:2)
                //   (F:1)
            }

            return frequencies.First();
        }
    }
}
