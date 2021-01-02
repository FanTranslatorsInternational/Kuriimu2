using System;
using System.IO;
using Kompression.Implementations.Encoders.Headerless;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;
using Kontract.Models.IO;

namespace Kompression.Implementations.Encoders.Nintendo
{
    // TODO: Find configuration way for bit depths and nibble order
    public class HuffmanEncoder : IHuffmanEncoder
    {
        private readonly int _bitDepth;

        private readonly HuffmanHeaderlessEncoder _encoder;

        public HuffmanEncoder(int bitDepth, NibbleOrder nibbleOrder = NibbleOrder.LowNibbleFirst)
        {
            _bitDepth = bitDepth;
            _encoder = new HuffmanHeaderlessEncoder(bitDepth, nibbleOrder);
        }

        public void Configure(IInternalHuffmanOptions huffmanOptions)
        {
            _encoder.Configure(huffmanOptions);
        }

        public void Encode(Stream input, Stream output, IHuffmanTreeBuilder treeBuilder)
        {
            if (input.Length > 0xFFFFFF)
                throw new InvalidOperationException("Data to compress is too long.");

            var compressionHeader = new[] { (byte)(0x20 + _bitDepth), (byte)input.Length, (byte)((input.Length >> 8) & 0xFF), (byte)((input.Length >> 16) & 0xFF) };
            output.Write(compressionHeader, 0, 4);

            _encoder.Encode(input, output, treeBuilder);
        }

        public void Dispose()
        {
            // nothing to dispose
        }
    }
}
