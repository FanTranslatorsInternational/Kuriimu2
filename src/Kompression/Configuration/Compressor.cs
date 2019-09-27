using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Huffman;
using Kompression.IO;
using Kompression.PatternMatch;

namespace Kompression.Configuration
{
    public class Compressor : ICompression
    {
        private IEncoder _encoder;
        private IDecoder _decoder;

        private int _preBufferSize;
        private bool _isBackwards;

        public string[] Names { get; }

        internal Compressor(IEncoder encoder, IDecoder decoder, int preBufferSize, bool isBackwards)
        {
            _encoder = encoder;
            _decoder = decoder;

            _preBufferSize = preBufferSize;
            _isBackwards = isBackwards;
        }

        public void Decompress(Stream input, Stream output)
        {
            if (_decoder == null)
                throw new InvalidOperationException("The decoder is not set.");

            _decoder.Decode(input, output);
        }

        public void Compress(Stream input, Stream output)
        {
            if (_encoder == null)
                throw new InvalidOperationException("The encoder is not set.");

            _encoder.Encode(input, output);
        }
    }
}
