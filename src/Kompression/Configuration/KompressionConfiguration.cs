using System;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Configuration
{
    /// <summary>
    /// The main configuration to configure an <see cref="ICompression"/>.
    /// </summary>
    public class KompressionConfiguration
    {
        private MatchOptions _matchOptions = new MatchOptions();
        private HuffmanOptions _huffmanOptions = new HuffmanOptions();

        private int _compressionMode;
        private Func<IMatchParser, IHuffmanTreeBuilder, int, IEncoder> _encoderFactory;
        private Func<int, IDecoder> _decoderFactory;

        /// <summary>
        /// Sets the compression modes used for de-/compressions.
        /// </summary>
        /// <param name="mode">The compression mode.</param>
        /// <returns>The configuration object.</returns>
        public KompressionConfiguration WithCompressionMode(int mode)
        {
            _compressionMode = mode;
            return this;
        }

        /// <summary>
        /// Sets the factory to create an <see cref="IEncoder"/>.
        /// </summary>
        /// <param name="encoderFactory">The factory to create an <see cref="IEncoder"/>.</param>
        /// <returns>The configuration object.</returns>
        public KompressionConfiguration EncodeWith(Func<IMatchParser, IHuffmanTreeBuilder, int, IEncoder> encoderFactory)
        {
            _encoderFactory = encoderFactory;
            return this;
        }

        /// <summary>
        /// Sets the factory to create an <see cref="IDecoder"/>.
        /// </summary>
        /// <param name="decoderFactory">The factory to create an <see cref="IDecoder"/>.</param>
        /// <returns>The configuration object.</returns>
        public KompressionConfiguration DecodeWith(Func<int, IDecoder> decoderFactory)
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
            configure(_huffmanOptions);

            return this;
        }

        /// <summary>
        /// BuildOptions the current configuration to an <see cref="ICompression"/>.
        /// </summary>
        /// <returns>The <see cref="ICompression"/> for this configuration.</returns>
        public ICompression Build()
        {
            // Get match parser for lempel-ziv encodings
            var matchParser = _matchOptions?.BuildMatchParser();

            // Get created instances for huffman encodings
            var huffmanTreeBuilder = _huffmanOptions?.BuildHuffmanTree();

            // Get created de-/compression instances
            var decoder = _decoderFactory?.Invoke(_compressionMode);
            var encoder = _encoderFactory?.Invoke(matchParser, huffmanTreeBuilder, _compressionMode);

            return new Compressor(encoder, decoder);
        }
    }
}
