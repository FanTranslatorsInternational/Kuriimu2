using System.IO;
using Kompression.Huffman.Decoders;
using Kompression.Huffman.Encoders;
using Kompression.Huffman.Support;

namespace Kompression.Huffman
{
    public abstract class BaseHuffman : ICompression
    {
        protected abstract int BitDepth { get; }

        protected abstract ByteOrder ByteOrder { get; }

        protected abstract IHuffmanEncoder CreateEncoder();

        protected abstract IHuffmanDecoder CreateDecoder();

        public void Decompress(Stream input, Stream output)
        {
            var decoder = CreateDecoder();
            decoder.Decode(input, output);
            decoder.Dispose();
        }

        public void Compress(Stream input, Stream output)
        {
            var inputArray = ToArray(input);

            var tree = new HuffmanTree(BitDepth, ByteOrder);
            var rootNode = tree.Build(inputArray);

            var encoder = CreateEncoder();
            encoder.Encode(inputArray, rootNode, output);
            encoder.Dispose();
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
