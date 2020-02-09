using System;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Configuration
{
    /// <summary>
    /// The main configuration to configure an <see cref="ICompression"/>.
    /// </summary>
    public class KompressionConfiguration : IKompressionConfiguration
    {
        private MatchOptions _matchOptions = new MatchOptions();
        private HuffmanOptions _huffmanOptions = new HuffmanOptions();

        private Func<IMatchParser, IHuffmanTreeBuilder, IEncoder> _encoderFactory;
        private Func<IDecoder> _decoderFactory;

        /// <summary>
        /// Sets the factory to create an <see cref="IEncoder"/>.
        /// </summary>
        /// <param name="encoderFactory">The factory to create an <see cref="IEncoder"/>.</param>
        /// <returns>The configuration object.</returns>
        public IKompressionConfiguration EncodeWith(Func<IMatchParser, IHuffmanTreeBuilder, IEncoder> encoderFactory)
        {
            _encoderFactory = encoderFactory;
            return this;
        }

        /// <summary>
        /// Sets the factory to create an <see cref="IDecoder"/>.
        /// </summary>
        /// <param name="decoderFactory">The factory to create an <see cref="IDecoder"/>.</param>
        /// <returns>The configuration object.</returns>
        public IKompressionConfiguration DecodeWith(Func<IDecoder> decoderFactory)
        {
            _decoderFactory = decoderFactory;
            return this;
        }

        /// <summary>
        /// Sets and modifies the configuration to find and search pattern matches.
        /// </summary>
        /// <param name="configure">The action to configure pattern match operations.</param>
        /// <returns>The configuration object.</returns>
        public IKompressionConfiguration WithMatchOptions(Action<IMatchOptions> configure)
        {
            configure(_matchOptions);

            return this;
        }

        /// <summary>
        /// Sets and modifies the configuration for huffman encodings.
        /// </summary>
        /// <param name="configure">The action to configure huffman encoding operations.</param>
        /// <returns>The configuration object.</returns>
        public IKompressionConfiguration WithHuffmanOptions(Action<IHuffmanOptions> configure)
        {
            configure(_huffmanOptions);

            return this;
        }

        /// <summary>
        /// Builds the current configuration to an <see cref="ICompression"/>.
        /// </summary>
        /// <returns>The <see cref="ICompression"/> for this configuration.</returns>
        public ICompression Build()
        {
            // Get match parser for lempel-ziv encodings
            var matchParser = _matchOptions?.BuildMatchParser();

            // Get created instances for huffman encodings
            var huffmanTreeBuilder = _huffmanOptions?.BuildHuffmanTree();

            // Get created de-/compression instances
            var decoder = _decoderFactory?.Invoke();
            var encoder = _encoderFactory?.Invoke(matchParser, huffmanTreeBuilder);

            return new Compressor(encoder, decoder);
        }
    }
}
