using Komponent.Contract.Enums;
using Komponent.IO;
using Kompression.Contract.Configuration;
using Kompression.Contract.DataClasses.Encoder.Huffman;
using Kompression.Contract.Encoder;
using Kompression.Contract.Encoder.Huffman;
using Kompression.Contract.Enums.Encoder.Huffman;
using Kompression.Extensions;

namespace Kompression.Encoder.Headerless
{
    public class HuffmanHeaderlessEncoder(int bitDepth, NibbleOrder nibbleOrder) : IHuffmanEncoder
    {
        public void Configure(IHuffmanEncoderOptionsBuilder huffmanOptions)
        {
        }

        public void Encode(Stream input, Stream output, IHuffmanTreeBuilder treeBuilder)
        {
            var rootNode = treeBuilder.Build(input.ToArray(), bitDepth, nibbleOrder);
            if (rootNode == null)
                return;

            // For a more even distribution of the children over the branches, we'll label the tree nodes
            var labelList = LabelTreeNodes(rootNode);

            // Create huffman bit codes
            var bitCodes = labelList[0].GetHuffCodes().ToDictionary(node => node.Item1, node => node.Item2);

            using var bw = new BinaryWriterX(output, true);

            // Write header
            bw.Write((byte)labelList.Count);

            // Write Huffman tree
            foreach (var node in labelList.Take(1).Concat(labelList.SelectMany(node => node.Children ?? [])))
            {
                if (node.Children != null)
                    node.Code |= node.Children.Select((child, i) => child.IsLeaf ? (byte)(0x80 >> i) : 0).Sum();
                bw.Write((byte)node.Code);
            }

            // Write bits to stream
            using var bitWriter = new BinaryBitWriter(bw.BaseStream, BitOrder.MostSignificantBitFirst, 4, ByteOrder.LittleEndian);
            switch (bitDepth)
            {
                case 4:
                    while (input.Position < input.Length)
                    {
                        var value = input.ReadByte();
                        if (nibbleOrder == NibbleOrder.LowNibbleFirst)
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

        private static List<HuffmanTreeNode> LabelTreeNodes(HuffmanTreeNode rootNode)
        {
            var labelList = new List<HuffmanTreeNode>();
            var frequencies = new List<HuffmanTreeNode> { rootNode };

            while (frequencies.Count > 0)
            {
                // Assign a score to each frequency node in the root
                var scores = frequencies.Select((freq, i) => new { Node = freq, Score = freq.Code - i });

                // Get node with lowest score
                var node = scores.OrderBy(freq => freq.Score).First().Node;

                // Remove that node from the tree root
                frequencies.Remove(node);

                node.Code = labelList.Count - node.Code;
                labelList.Add(node);

                // Loop through all children that aren't leaves
                if (node.Children == null)
                    continue;

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
