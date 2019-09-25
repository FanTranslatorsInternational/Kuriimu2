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

        private MatchOptions _matchOptions;
        private HuffmanOptions _huffmanOptions;

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

        public Compressor Build()
        {
            var priceCalculator = _matchOptions?.PriceCalculatorFactory?.Invoke();
            var limits = _matchOptions?.MatchFinderOptions?.LimitFactories?.Where(factory => factory != null)
                .Select(factory => factory.Invoke()).ToList();
            var matchFinders = _matchOptions?.MatchFinderOptions?.MatchFinderFactories?.
                Where(factory => factory != null).Select(factory => factory.Invoke(limits)).ToList();
            var skipAfterMatch = _matchOptions?.MatchParserOptions?.SkipAfterMatch ?? 0;
            var matchParser = _matchOptions?.MatchParserOptions?.
                MatchParserFactory?.Invoke(matchFinders, priceCalculator, skipAfterMatch);

            var preBufferSize = _matchOptions?.MatchParserOptions?.PreBufferSize ?? 0;
            var isBackwards = _matchOptions?.MatchFinderOptions?.FindBackwards ?? false;

            var huffmanTreeBuilder = _huffmanOptions?.TreeBuilderFactory?.Invoke();

            var decoder = _decoderFactory?.Invoke(_compressionModes);
            var encoder = _encoderFactory?.Invoke(matchParser, huffmanTreeBuilder, _compressionModes);

            return new Compressor(encoder, decoder, preBufferSize, isBackwards);
        }
    }
}
