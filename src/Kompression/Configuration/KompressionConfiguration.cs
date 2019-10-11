using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Kompression.Interfaces;
using Kompression.Models;

[assembly: InternalsVisibleTo("KompressionUnitTests")]

namespace Kompression.Configuration
{
    /// <summary>
    /// The main configuration to configure an <see cref="ICompression"/>.
    /// </summary>
    public class KompressionConfiguration
    {
        private MatchOptions _matchOptions;
        private HuffmanOptions _huffmanOptions;

        private int[] _compressionModes;
        private Func<IMatchParser, IHuffmanTreeBuilder, int[], IEncoder> _encoderFactory;
        private Func<int[], IDecoder> _decoderFactory;

        /// <summary>
        /// Sets the compression modes used for de-/compressions.
        /// </summary>
        /// <param name="modes">The compression modes.</param>
        /// <returns>The configuration object.</returns>
        public KompressionConfiguration WithCompressionModes(params int[] modes)
        {
            _compressionModes = modes;
            return this;
        }

        /// <summary>
        /// Sets the factory to create an <see cref="IEncoder"/>.
        /// </summary>
        /// <param name="encoderFactory">The factory to create an <see cref="IEncoder"/>.</param>
        /// <returns>The configuration object.</returns>
        public KompressionConfiguration EncodeWith(Func<IMatchParser, IHuffmanTreeBuilder, int[], IEncoder> encoderFactory)
        {
            _encoderFactory = encoderFactory;
            return this;
        }

        /// <summary>
        /// Sets the factory to create an <see cref="IDecoder"/>.
        /// </summary>
        /// <param name="decoderFactory">The factory to create an <see cref="IDecoder"/>.</param>
        /// <returns>The configuration object.</returns>
        public KompressionConfiguration DecodeWith(Func<int[], IDecoder> decoderFactory)
        {
            _decoderFactory = decoderFactory;
            return this;
        }

        /// <summary>
        /// Sets and modifies the configuration to find and search pattern matches.
        /// </summary>
        /// <param name="configure">The action to configure pattern match operations.</param>
        /// <returns>The configuration object.</returns>
        public KompressionConfiguration WithMatchOptions(Action<IMatchOptions> configure)
        {
            if (_matchOptions == null)
                _matchOptions = new MatchOptions();

            configure(_matchOptions);

            return this;
        }

        /// <summary>
        /// Sets and modifies the configuration for huffman encodings.
        /// </summary>
        /// <param name="configure">The action to configure huffman encoding operations.</param>
        /// <returns>The configuration object.</returns>
        public KompressionConfiguration WithHuffmanOptions(Action<IHuffmanOptions> configure)
        {
            if (_huffmanOptions == null)
                _huffmanOptions = new HuffmanOptions();

            configure(_huffmanOptions);

            return this;
        }

        /// <summary>
        /// Build the current configuration to an <see cref="ICompression"/>.
        /// </summary>
        /// <returns>The <see cref="ICompression"/> for this configuration.</returns>
        public ICompression Build()
        {
            // Get or default values for pattern match operations
            var preBufferSize = _matchOptions?.PreBufferSize ?? 0;
            var isBackwards = _matchOptions?.FindBackwards ?? false;
            var skipAfterMatch = _matchOptions?.SkipAfterMatch ?? 0;
            var dataType = _matchOptions?.UnitSize ?? UnitSize.Byte;
            var taskCount = _matchOptions?.TaskCount ?? 8;
            var findOptions = new FindOptions(isBackwards, preBufferSize, skipAfterMatch, dataType, taskCount);

            // Get created instances for pattern match operations
            var priceCalculator = _matchOptions?.PriceCalculatorFactory?.Invoke();
            var limits = _matchOptions?.LimitFactories?.
                Where(factory => factory != null).
                Select(factory => factory()).
                ToArray();
            var matchFinders = _matchOptions?.MatchFinderFactories?.
                Where(factory => factory != null).
                Select(factory => factory(limits, findOptions)).
                ToArray();
            var matchParser = _matchOptions?.MatchParserFactory?.Invoke(matchFinders, priceCalculator, findOptions);

            // Get created instances for huffman encodings
            var huffmanTreeBuilder = _huffmanOptions?.TreeBuilderFactory?.Invoke();

            // Get created de-/compression instances
            var decoder = _decoderFactory?.Invoke(_compressionModes);
            var encoder = _encoderFactory?.Invoke(matchParser, huffmanTreeBuilder, _compressionModes);

            return new Compressor(encoder, decoder);
        }
    }
}
