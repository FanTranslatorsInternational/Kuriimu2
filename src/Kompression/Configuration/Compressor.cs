using System;
using System.IO;
using Kontract;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Configuration
{
    /// <summary>
    /// The main <see cref="ICompression"/> which gets created by <see cref="KompressionConfiguration"/>.
    /// </summary>
    class Compressor : ICompression
    {
        private readonly Func<IDecoder> _decoderAction;
        private readonly Func<IEncoder> _encoderAction;
        private readonly Func<ILzEncoder> _lzEncoderAction;
        private readonly Func<IHuffmanEncoder> _huffmanEncoderAction;
        private readonly Func<ILzHuffmanEncoder> _lzHuffmanEncoderAction;

        private readonly Func<IMatchParser> _matchParserAction;
        private readonly Func<IHuffmanTreeBuilder> _treeBuilderAction;

        /// <inheritdoc cref="Names"/>
        public string[] Names { get; }

        /// <summary>
        /// Creates a new instance of <see cref="Compressor"/>.
        /// </summary>
        /// <param name="decoderAction">The <see cref="IDecoder"/> to use with the decompression action.</param>
        /// <param name="encoderAction">The <see cref="IEncoder"/> to use with the compression action.</param>
        public Compressor(Func<IDecoder> decoderAction, Func<IEncoder> encoderAction)
        {
            ContractAssertions.IsNotNull(decoderAction, nameof(decoderAction));
            ContractAssertions.IsNotNull(encoderAction, nameof(encoderAction));

            _decoderAction = decoderAction;
            _encoderAction = encoderAction;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Compressor"/>.
        /// </summary>
        /// <param name="decoderAction">The <see cref="IDecoder"/> to use with the decompression action.</param>
        /// <param name="encoderAction">The <see cref="ILzEncoder"/> to use with the compression action.</param>
        /// <param name="matchParserAction">The <see cref="IMatchParser"/> to parse matches.</param>
        public Compressor(Func<IDecoder> decoderAction, Func<ILzEncoder> encoderAction, Func<IMatchParser> matchParserAction)
        {
            ContractAssertions.IsNotNull(decoderAction, nameof(decoderAction));
            ContractAssertions.IsNotNull(encoderAction, nameof(encoderAction));
            ContractAssertions.IsNotNull(matchParserAction, nameof(matchParserAction));

            _decoderAction = decoderAction;
            _lzEncoderAction = encoderAction;
            _matchParserAction = matchParserAction;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Compressor"/>.
        /// </summary>
        /// <param name="decoderAction">The <see cref="IDecoder"/> to use with the decompression action.</param>
        /// <param name="encoderAction">The <see cref="IHuffmanEncoder"/> to use with the compression action.</param>
        /// <param name="treeBuilderAction">The <see cref="IHuffmanTreeBuilder"/> to build the huffman tree.</param>
        public Compressor(Func<IDecoder> decoderAction, Func<IHuffmanEncoder> encoderAction, Func<IHuffmanTreeBuilder> treeBuilderAction)
        {
            ContractAssertions.IsNotNull(decoderAction, nameof(decoderAction));
            ContractAssertions.IsNotNull(encoderAction, nameof(encoderAction));
            ContractAssertions.IsNotNull(treeBuilderAction, nameof(treeBuilderAction));

            _decoderAction = decoderAction;
            _huffmanEncoderAction = encoderAction;
            _treeBuilderAction = treeBuilderAction;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Compressor"/>.
        /// </summary>
        /// <param name="decoderAction">The <see cref="IDecoder"/> to use with the decompression action.</param>
        /// <param name="encoderAction">The <see cref="ILzHuffmanEncoder"/> to use with the compression action.</param>
        /// <param name="matchParserAction">The <see cref="IMatchParser"/> to parse matches.</param>
        /// <param name="treeBuilderAction">The <see cref="IHuffmanTreeBuilder"/> to build the huffman tree.</param>
        public Compressor(Func<IDecoder> decoderAction, Func<ILzHuffmanEncoder> encoderAction, Func<IMatchParser> matchParserAction, Func<IHuffmanTreeBuilder> treeBuilderAction)
        {
            ContractAssertions.IsNotNull(decoderAction, nameof(decoderAction));
            ContractAssertions.IsNotNull(encoderAction, nameof(encoderAction));
            ContractAssertions.IsNotNull(matchParserAction, nameof(matchParserAction));
            ContractAssertions.IsNotNull(treeBuilderAction, nameof(treeBuilderAction));

            _decoderAction = decoderAction;
            _lzHuffmanEncoderAction = encoderAction;
            _matchParserAction = matchParserAction;
            _treeBuilderAction = treeBuilderAction;
        }

        /// <inheritdoc cref="Decompress"/>
        public void Decompress(Stream input, Stream output)
        {
            var decoder = _decoderAction();

            if (decoder == null)
                throw new InvalidOperationException("The decoder is not set.");

            decoder.Decode(input, output);
        }

        /// <inheritdoc cref="Compress"/>
        public void Compress(Stream input, Stream output)
        {
            if (_lzEncoderAction != null)
            {
                var lzEncoder = _lzEncoderAction();
                ContractAssertions.IsNotNull(lzEncoder, nameof(lzEncoder));

                var matchParser = _matchParserAction();

                lzEncoder.Encode(input, output, matchParser.ParseMatches(input));
            }
            else if (_huffmanEncoderAction != null)
            {
                var huffmanEncoder = _huffmanEncoderAction();
                ContractAssertions.IsNotNull(huffmanEncoder, nameof(huffmanEncoder));

                var treeBuilder = _treeBuilderAction();

                huffmanEncoder.Encode(input, output, treeBuilder);
            }
            else if (_lzHuffmanEncoderAction != null)
            {
                var lzHuffmanEncoder = _lzHuffmanEncoderAction();
                ContractAssertions.IsNotNull(lzHuffmanEncoder, nameof(lzHuffmanEncoder));

                var matchParser = _matchParserAction();
                var treeBuilder = _treeBuilderAction();

                lzHuffmanEncoder.Encode(input, output, matchParser.ParseMatches(input), treeBuilder);
            }
            else
            {
                var encoder = _encoderAction();
                ContractAssertions.IsNotNull(encoder, nameof(encoder));

                encoder.Encode(input, output);
            }
        }

        #region Dispose

        public void Dispose()
        {
            // Nothing to dispose
        }

        #endregion
    }
}
