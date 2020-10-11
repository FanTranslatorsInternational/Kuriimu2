using System;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Configuration
{
    /// <summary>
    /// The main configuration to configure an <see cref="ICompression"/>.
    /// </summary>
    public class KompressionConfiguration : IKompressionConfiguration, ILzHuffmanKompressionConfiguration
    {
        private MatchOptions _matchOptions = new MatchOptions();
        private HuffmanOptions _huffmanOptions = new HuffmanOptions();

        private Func<IEncoder> _encoderFactory;
        private Func<ILzEncoder> _lzEncoderFactory;
        private Func<IHuffmanEncoder> _huffmanEncoderFactory;
        private Func<ILzHuffmanEncoder> _lzHuffmanEncoderFactory;
        private Func<IDecoder> _decoderFactory;

        /// <inheritdoc cref="DecodeWith"/>
        public IKompressionConfiguration DecodeWith(Func<IDecoder> decoderFactory)
        {
            _decoderFactory = decoderFactory;
            return this;
        }

        /// <inheritdoc cref="EncodeWith(Func{IEncoder})"/>
        public IKompressionConfiguration EncodeWith(Func<IEncoder> encoderFactory)
        {
            _encoderFactory = encoderFactory;
            _lzEncoderFactory = null;
            _huffmanEncoderFactory = null;
            _lzHuffmanEncoderFactory = null;

            return this;
        }

        /// <inheritdoc cref="EncodeWith(Func{ILzEncoder})"/>
        public ILzKompressionConfiguration EncodeWith(Func<ILzEncoder> encoderFactory)
        {
            _encoderFactory = null;
            _lzEncoderFactory = encoderFactory;
            _huffmanEncoderFactory = null;
            _lzHuffmanEncoderFactory = null;

            return this;
        }

        /// <inheritdoc cref="EncodeWith(Func{IHuffmanEncoder})"/>
        public IHuffmanKompressionConfiguration EncodeWith(Func<IHuffmanEncoder> encoderFactory)
        {
            _encoderFactory = null;
            _lzEncoderFactory = null;
            _huffmanEncoderFactory = encoderFactory;
            _lzHuffmanEncoderFactory = null;

            return this;
        }

        /// <inheritdoc cref="EncodeWith(Func{ILzHuffmanEncoder})"/>
        public ILzHuffmanKompressionConfiguration EncodeWith(Func<ILzHuffmanEncoder> encoderFactory)
        {
            _encoderFactory = null;
            _lzEncoderFactory = null;
            _huffmanEncoderFactory = null;
            _lzHuffmanEncoderFactory = encoderFactory;

            return this;
        }

        /// <summary>
        /// Sets and modifies the configuration to find and search pattern matches.
        /// </summary>
        /// <param name="configure">The action to configure pattern match operations.</param>
        /// <returns>The configuration object.</returns>
        public IKompressionConfiguration ConfigureLz(Action<IMatchOptions> configure)
        {
            configure(_matchOptions);
            return this;
        }

        /// <summary>
        /// Sets and modifies the configuration for huffman encodings.
        /// </summary>
        /// <param name="configure">The action to configure huffman encoding operations.</param>
        /// <returns>The configuration object.</returns>
        public IKompressionConfiguration ConfigureHuffman(Action<IHuffmanOptions> configure)
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
            if (_lzEncoderFactory != null)
                return new Compressor(_decoderFactory, _lzEncoderFactory, _matchOptions.BuildMatchParser);

            if (_huffmanEncoderFactory != null)
                return new Compressor(_decoderFactory, _huffmanEncoderFactory, _huffmanOptions.BuildHuffmanTree);

            if (_lzHuffmanEncoderFactory != null)
                return new Compressor(_decoderFactory, _lzHuffmanEncoderFactory, _matchOptions.BuildMatchParser, _huffmanOptions.BuildHuffmanTree);

            return new Compressor(_decoderFactory, _encoderFactory);
        }

        /// <summary>
        /// Creates a new chain of decoding instances.
        /// </summary>
        /// <returns>The new decoder</returns>
        private IDecoder BuildDecoder()
        {
            return _decoderFactory?.Invoke();
        }
    }
}
