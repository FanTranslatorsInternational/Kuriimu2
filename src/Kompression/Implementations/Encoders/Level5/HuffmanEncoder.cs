using System;
using System.IO;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;
using Kontract.Models.IO;

namespace Kompression.Implementations.Encoders.Level5
{
    public class HuffmanEncoder : IHuffmanEncoder
    {
        private readonly int _bitDepth;

        private readonly HuffmanHeaderlessEncoder _encoder;

        public HuffmanEncoder(int bitDepth, NibbleOrder nibbleOrder)
        {
            _bitDepth = bitDepth;
            _encoder = new HuffmanHeaderlessEncoder(bitDepth, nibbleOrder);
        }

        public void Encode(Stream input, Stream output, IHuffmanTreeBuilder treeBuilder)
        {
            if (input.Length > 0x1FFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var huffmanMode = _bitDepth == 4 ? 2 : 3;
            var compressionHeader = new[] {
                (byte)((byte)(input.Length << 3) | huffmanMode),
                (byte)(input.Length >> 5),
                (byte)(input.Length >> 13),
                (byte)(input.Length >> 21) };
            output.Write(compressionHeader, 0, 4);

            _encoder.Encode(input, output, treeBuilder);
        }

        public void Dispose()
        {
            // nothing to dispose
        }
    }
}
