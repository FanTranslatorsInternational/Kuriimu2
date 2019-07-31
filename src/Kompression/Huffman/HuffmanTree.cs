using System;
using System.Collections.Generic;
using System.Linq;
using Kompression.Exceptions;

namespace Kompression.Huffman
{
    class HuffmanTree
    {
        private readonly int _bitDepth;
        private readonly ByteOrder? _byteOrder;

        public HuffmanTree(int bitDepth)
        {
            if (bitDepth <= 0)
                throw new InvalidOperationException("BitDepth needs to be > 0.");
            if (bitDepth < 8)
                throw new InvalidOperationException("ByteOrder is needed for BitDepths < 8.");
            if (!IsPowerOf2(bitDepth))
                throw new InvalidOperationException("BitDepth needs to be a power of 2.");

            _bitDepth = bitDepth;
        }

        public HuffmanTree(int bitDepth, ByteOrder byteOrder)
        {
            if (bitDepth <= 0)
                throw new InvalidOperationException("BitDepth needs to be > 0.");
            if (!IsPowerOf2(bitDepth))
                throw new InvalidOperationException("BitDepth needs to be a power of 2.");

            _bitDepth = bitDepth;
            _byteOrder = byteOrder;
        }

        public List<HuffmanTreeNode> Build(byte[] input)
        {
            // Get value frequencies of input
            var frequencies = GetFrequencies(input, _bitDepth, _byteOrder).ToList();

            // Add a stub entry in the special case that there's only one item;
            // We want at least 2 elements in the tree to encode
            if (frequencies.Count == 1)
                frequencies.Add(new HuffmanTreeNode(0) { Code = input[0] + 1 });

            // Create and sort the tree of frequencies
            frequencies = CreateAndSortTree(frequencies);

            // For a more even distribution of the children over the branches, we'll label the tree nodes
            var labelList = LabelTreeNodes(frequencies);

            return labelList;
        }

        private static bool IsPowerOf2(int value)
        {
            return (value & (value - 1)) == 0;
        }

        private static IEnumerable<HuffmanTreeNode> GetFrequencies(byte[] input, int bitDepth, ByteOrder? byteOrder)
        {
            if (bitDepth != 4 && bitDepth != 8)
                throw new BitDepthNotSupportedException(bitDepth);

            // Split input into elements of bitDepth size
            IEnumerable<byte> data = input;
            if (bitDepth == 4)
                if (byteOrder == ByteOrder.LittleEndian)
                    data = input.SelectMany(b => new[] { (byte)(b % 16), (byte)(b / 16) });
                else
                    data = input.SelectMany(b => new[] { (byte)(b / 16), (byte)(b % 16) });

            // Group elements by value
            var groupedElements = data.GroupBy(b => b);

            // Create huffman nodes out of value groups
            return groupedElements.Select(group => new HuffmanTreeNode(group.Count()) { Code = group.Key });
        }

        private static List<HuffmanTreeNode> CreateAndSortTree(List<HuffmanTreeNode> frequencies)
        {
            //Sort and create the tree
            while (frequencies.Count > 1)
            {
                // Order frequencies ascending
                frequencies = frequencies.OrderBy(n => n.Frequency).ToList();

                // Create new tree node with the 2 elements of least frequency
                var leastFrequencyNode = new HuffmanTreeNode(frequencies[0].Frequency + frequencies[1].Frequency)
                {
                    Children = frequencies.Take(2).ToArray()
                };

                // Remove those least frequency elements and append new tree node to frequencies
                frequencies = frequencies.Skip(2).Concat(new[] { leastFrequencyNode }).ToList();

                // This ultimately results in a tree like structure where the most frequent elements are closer to the root;
                // while less frequent elements are closer to the leafs

                // Example:
                // (F:4)
                //   (F:3)
                //     (F:1)
                //     (F:2)
                //   (F:1)
            }

            return frequencies;
        }

        private static List<HuffmanTreeNode> LabelTreeNodes(List<HuffmanTreeNode> frequencies)
        {
            var labelList = new List<HuffmanTreeNode>();

            while (frequencies.Any())
            {
                // Assign a score to each frequency node in the root
                var scores = frequencies.Select((freq, i) => new { Node = freq, Score = freq.Code - i });

                // Get node with lowest score
                var node = scores.OrderBy(freq => freq.Score).First().Node;

                // Remove that node from the tree root
                frequencies.Remove(node);

                // TODO: Understand what this portion does
                node.Code = labelList.Count - node.Code;
                labelList.Add(node);
                // Loop through all children that aren't leaves
                foreach (var child in node.Children.Reverse().Where(child => !child.IsLeaf))
                {
                    child.Code = labelList.Count;
                    frequencies.Add(child);
                }
            }

            return labelList;
        }
    }
}
