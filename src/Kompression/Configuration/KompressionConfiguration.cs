using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Huffman;
using Kompression.PatternMatch;

namespace Kompression.Configuration
{
    public class KompressionConfiguration
    {
        private Func<IMatchParser, IHuffmanTreeBuilder, int[], IEncoder> _encoderFactory;
        private Func<int[], IDecoder> _decoderFactory;
        private int[] _compressionModes;

        private IMatchOptions _matchOptions;
        private IHuffmanOptions _huffmanOptions;

        public KompressionConfiguration WithCompressionModes(params int[] modes)
        {
            _compressionModes = modes;
            return this;
        }

        public KompressionConfiguration EncodeWith(Func<IMatchParser, IHuffmanTreeBuilder, int[], IEncoder> encoderFactory)
        {
            _encoderFactory = encoderFactory;
            return this;
        }

        public KompressionConfiguration DecodeWith(Func<int[], IDecoder> decoderFactory)
        {
            _decoderFactory = decoderFactory;
            return this;
        }

        public KompressionConfiguration WithMatchOptions(Action<IMatchOptions> configure)
        {
            if (_matchOptions == null)
                _matchOptions = new MatchOptions();
            configure(_matchOptions);
            return this;
        }

        public KompressionConfiguration WithHuffmanOptions(Action<IHuffmanOptions> configure)
        {
            if (_huffmanOptions == null)
                _huffmanOptions = new HuffmanOptions();
            configure(_huffmanOptions);
            return this;
        }

        public ICompression Build()
        {
            // TODO: Build the configuration into an executable ICompression implementation
            return null;
        }
    }
}
