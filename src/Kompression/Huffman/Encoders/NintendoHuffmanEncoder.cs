using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kompression.Huffman.Encoders
{
    class NintendoHuffmanEncoder : IHuffmanEncoder
    {
        private readonly int _bitDepth;

        public NintendoHuffmanEncoder(int bitDepth)
        {
            _bitDepth = bitDepth;
        }

        public void Encode(byte[] input, List<HuffmanTreeNode> labelList, Stream output)
        {
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
                    bw.Write(node.Code);
                }

                // Write bits to stream
                using (var bitWriter = new BitWriter(bw.BaseStream, BitOrder.MSBFirst))
                {
                    foreach (var bit in input.SelectMany(b => bitCodes[b]))
                        bitWriter.WriteBit(bit - '0');
                    bitWriter.Flush();
                }
            }
        }

        public void Dispose()
        {
            // nothing to dispose
        }
    }
}
