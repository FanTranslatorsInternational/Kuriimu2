using System;
using System.IO;
using System.Linq;

namespace Kompression.Huffman
{
    public class Huffman4LE : ICompression
    {
        public void Decompress(Stream input, Stream output)
        {
            throw new NotImplementedException();
        }

        // TODO: Finish huffman compression
        public void Compress(Stream input, Stream output)
        {
            var inputArray = ToArray(input);

            var tree = new HuffmanTree(4, ByteOrder.LittleEndian);
            var labelList = tree.Build(inputArray);

            // Create huffman bit codes
            var bitCodes = labelList[0].GetHuffCodes().ToDictionary(node => node.Item1, node => node.Item2);
        }

        private byte[] ToArray(Stream input)
        {
            var bkPos = input.Position;
            var inputArray = new byte[input.Length];
            input.Read(inputArray, 0, inputArray.Length);
            input.Position = bkPos;

            return inputArray;
        }
    }
}
