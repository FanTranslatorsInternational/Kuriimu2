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

        private readonly IInternalMatchOptions _matchOptions;
        private readonly IInternalHuffmanOptions _huffmanOptions;

        /// <summary>
        /// Creates a new instance of <see cref="Compressor"/>.
        /// </summary>
        /// <param name="decoderAction">The <see cref="IDecoder"/> to use with the decompression action.</param>
        /// <param name="encoderAction">The <see cref="IEncoder"/> to use with the compression action.</param>
        public Compressor(Func<IDecoder> decoderAction, Func<IEncoder> encoderAction)
        {
            _decoderAction = decoderAction;
            _encoderAction = encoderAction;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Compressor"/>.
        /// </summary>
        /// <param name="decoderAction">The <see cref="IDecoder"/> to use with the decompression action.</param>
        /// <param name="encoderAction">The <see cref="ILzEncoder"/> to use with the compression action.</param>
        /// <param name="matchOptions">The <see cref="IInternalMatchOptions"/> to configure the matching options.</param>
        public Compressor(Func<IDecoder> decoderAction, Func<ILzEncoder> encoderAction, IInternalMatchOptions matchOptions)
        {
            ContractAssertions.IsNotNull(matchOptions, nameof(matchOptions));

            _decoderAction = decoderAction;
            _lzEncoderAction = encoderAction;
            _matchOptions = matchOptions;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Compressor"/>.
        /// </summary>
        /// <param name="decoderAction">The <see cref="IDecoder"/> to use with the decompression action.</param>
        /// <param name="encoderAction">The <see cref="IHuffmanEncoder"/> to use with the compression action.</param>
        /// <param name="huffmanOptions">The <see cref="IInternalHuffmanOptions"/> to configure the huffman options.</param>
        public Compressor(Func<IDecoder> decoderAction, Func<IHuffmanEncoder> encoderAction, IInternalHuffmanOptions huffmanOptions)
        {
            ContractAssertions.IsNotNull(huffmanOptions, nameof(huffmanOptions));

            _decoderAction = decoderAction;
            _huffmanEncoderAction = encoderAction;
            _huffmanOptions = huffmanOptions;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Compressor"/>.
        /// </summary>
        /// <param name="decoderAction">The <see cref="IDecoder"/> to use with the decompression action.</param>
        /// <param name="encoderAction">The <see cref="ILzHuffmanEncoder"/> to use with the compression action.</param>
        /// <param name="matchOptions">The <see cref="IInternalMatchOptions"/> to configure the matching options.</param>
        /// <param name="huffmanOptions">The <see cref="IInternalHuffmanOptions"/> to configure the huffman options.</param>
        public Compressor(Func<IDecoder> decoderAction, Func<ILzHuffmanEncoder> encoderAction, IInternalMatchOptions matchOptions, IInternalHuffmanOptions huffmanOptions)
        {
            ContractAssertions.IsNotNull(matchOptions, nameof(matchOptions));
            ContractAssertions.IsNotNull(huffmanOptions, nameof(huffmanOptions));

            _decoderAction = decoderAction;
            _lzHuffmanEncoderAction = encoderAction;
            _matchOptions = matchOptions;
            _huffmanOptions = huffmanOptions;
        }

        /// <inheritdoc cref="Decompress"/>
        public void Decompress(Stream input, Stream output)
        {
            var decoder = _decoderAction?.Invoke();

            if (decoder == null)
                throw new InvalidOperationException("No compression decoder was set.");

            decoder.Decode(input, output);
        }

        /// <inheritdoc cref="Compress"/>
        public void Compress(Stream input, Stream output)
        {
            if (_lzEncoderAction != null)
            {
                var lzEncoder = _lzEncoderAction();
                ContractAssertions.IsNotNull(lzEncoder, nameof(lzEncoder));

                lzEncoder.Configure(_matchOptions);
                var matchParser = _matchOptions.BuildMatchParser();

                lzEncoder.Encode(input, output, matchParser?.ParseMatches(input));
            }
            else if (_huffmanEncoderAction != null)
            {
                var huffmanEncoder = _huffmanEncoderAction();
                ContractAssertions.IsNotNull(huffmanEncoder, nameof(huffmanEncoder));

                huffmanEncoder.Configure(_huffmanOptions);
                var treeBuilder = _huffmanOptions.BuildHuffmanTree();

                huffmanEncoder.Encode(input, output, treeBuilder);
            }
            else if (_lzHuffmanEncoderAction != null)
            {
                var lzHuffmanEncoder = _lzHuffmanEncoderAction();
                ContractAssertions.IsNotNull(lzHuffmanEncoder, nameof(lzHuffmanEncoder));

                lzHuffmanEncoder.Configure(_matchOptions, _huffmanOptions);
                var matchParser = _matchOptions.BuildMatchParser();
                var treeBuilder = _huffmanOptions.BuildHuffmanTree();

                lzHuffmanEncoder.Encode(input, output, matchParser?.ParseMatches(input), treeBuilder);
            }
            else if (_encoderAction != null)
            {
                var encoder = _encoderAction();
                ContractAssertions.IsNotNull(encoder, nameof(encoder));

                encoder.Encode(input, output);
            }
            else
                throw new InvalidOperationException("No compression encoder was set.");
        }

        #region Dispose

        public void Dispose()
        {
            // Nothing to dispose
        }

        #endregion
    }
}
