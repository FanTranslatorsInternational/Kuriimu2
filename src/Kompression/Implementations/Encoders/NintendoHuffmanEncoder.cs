using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kompression.Configuration;
using Kompression.Extensions;
using Kompression.Huffman.Support;
using Kompression.Interfaces;
using Kompression.IO;

namespace Kompression.Implementations.Encoders
{
    class NintendoHuffmanEncoder : IEncoder
    {
        private readonly int _bitDepth;
        private IHuffmanTreeBuilder _treeBuilder;
        private ByteOrder _byteOrder;

        public NintendoHuffmanEncoder(int bitDepth, ByteOrder byteOrder, IHuffmanTreeBuilder treeBuilder)
        {
            _bitDepth = bitDepth;
            _treeBuilder = treeBuilder;
            _byteOrder = byteOrder;
        }

        public void Encode(Stream input, Stream output)
        {
            var rootNode = _treeBuilder.Build(input.ToArray(), _bitDepth, _byteOrder);

            // For a more even distribution of the children over the branches, we'll label the tree nodes
            var labelList = LabelTreeNodes(rootNode);

            // Create huffman bit codes
            var bitCodes = labelList[0].GetHuffCodes().ToDictionary(node => node.Item1, node => node.Item2);

            // Write header + tree
            using (var bw = new BinaryWriter(output))
            {
                // Write header
                var header = new[] { (byte)(0x20 + _bitDepth), (byte)(input.Length & 0xFF), (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
                bw.Write(header, 0, 4);
                bw.Write((byte)labelList.Count);

                // Write Huffman tree
                foreach (var node in labelList.Take(1).Concat(labelList.SelectMany(node => node.Children)))
                {
                    if (node.Children != null)
                        node.Code |= node.Children.Select((child, i) => child.IsLeaf ? (byte)(0x80 >> i) : 0).Sum();
                    bw.Write((byte)node.Code);
                }

                // Write bits to stream
                using (var bitWriter = new BitWriter(bw.BaseStream, BitOrder.MsbFirst, 4, ByteOrder.LittleEndian))
                {
                    switch (_bitDepth)
                    {
                        case 4:
                            while (input.Position < input.Length)
                            {
                                var value = input.ReadByte();
                                if (_byteOrder == ByteOrder.LittleEndian)
                                {
                                    foreach (var bit in bitCodes[value % 16])
                                        bitWriter.WriteBit(bit - '0');
                                    foreach (var bit in bitCodes[value / 16])
                                        bitWriter.WriteBit(bit - '0');
                                }
                                else
                                {
                                    foreach (var bit in bitCodes[value / 16])
                                        bitWriter.WriteBit(bit - '0');
                                    foreach (var bit in bitCodes[value % 16])
                                        bitWriter.WriteBit(bit - '0');
                                }
                            }
                            break;
                        case 8:
                            while (input.Position < input.Length)
                            {
                                var value = input.ReadByte();
                                foreach (var bit in bitCodes[value])
                                    bitWriter.WriteBit(bit - '0');
                            }

                            break;
                    }
                    bitWriter.Flush();
                }
            }
        }

        private List<HuffmanTreeNode> LabelTreeNodes(HuffmanTreeNode rootNode)
        {
            var labelList = new List<HuffmanTreeNode>();
            var frequencies = new List<HuffmanTreeNode> { rootNode };

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

        public void Dispose()
        {
            // nothing to dispose
        }
    }
}
