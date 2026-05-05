using Kompression.Configuration;
using Kompression.Contract;
using Kompression.Contract.DataClasses.Encoder.LempelZiv;
using Kompression.Contract.Decoder;
using Kompression.Contract.Encoder;
using Kompression.Contract.Encoder.Huffman;
using Kompression.Contract.Encoder.LempelZiv.MatchParser;
using Kompression.DataClasses.Configuration;

namespace Kompression
{
    /// <summary>
    /// The main <see cref="ICompression"/> which gets created by <see cref="CompressionConfigurationBuilder"/>.
    /// </summary>
    internal class Compression : ICompression
    {
        private readonly CompressionConfigurationOptions _options;

        private readonly LempelZivEncoderOptionsBuilder _lempelZivBuilder;
        private readonly HuffmanEncoderOptionsBuilder _huffmanBuilder;

        /// <summary>
        /// Creates a new instance of <see cref="Compression"/>.
        /// </summary>
        /// <param name="options">The <see cref="CompressionConfigurationOptions"/> to use.</param>
        public Compression(CompressionConfigurationOptions options)
        {
            _options = options;

            _lempelZivBuilder = new LempelZivEncoderOptionsBuilder(_options.LempelZiv);
            _huffmanBuilder = new HuffmanEncoderOptionsBuilder(_options.Huffman);
        }

        /// <inheritdoc cref="Decompress"/>
        public void Decompress(Stream input, Stream output)
        {
            ArgumentNullException.ThrowIfNull(_options.DecoderOptions.DecoderDelegate);

            IDecoder decoder = _options.DecoderOptions.DecoderDelegate();
            decoder.Decode(input, output);
        }

        /// <inheritdoc cref="Compress"/>
        public void Compress(Stream input, Stream output)
        {
            if (_options.EncoderOptions.LempelZivEncoderDelegate != null)
            {
                ILempelZivEncoder encoder = _options.EncoderOptions.LempelZivEncoderDelegate();
                CompressLempelZiv(input, output, encoder);
            }
            else if (_options.EncoderOptions.HuffmanEncoderDelegate != null)
            {
                IHuffmanEncoder encoder = _options.EncoderOptions.HuffmanEncoderDelegate();
                CompressHuffman(input, output, encoder);
            }
            else if (_options.EncoderOptions.LempelZivHuffmanEncoderDelegate != null)
            {
                ILempelZivHuffmanEncoder encoder = _options.EncoderOptions.LempelZivHuffmanEncoderDelegate();
                CompressLempelZivHuffman(input, output, encoder);
            }
            else if (_options.EncoderOptions.EncoderDelegate != null)
            {
                IEncoder encoder = _options.EncoderOptions.EncoderDelegate();
                encoder.Encode(input, output);
            }
            else
                throw new InvalidOperationException("No compression encoder was set.");
        }

        private void CompressLempelZiv(Stream input, Stream output, ILempelZivEncoder encoder)
        {
            encoder.Configure(_lempelZivBuilder);

            ILempelZivMatchParser matchParser = _lempelZivBuilder.Build();
            IEnumerable<LempelZivMatch> matches = matchParser.ParseMatches(input);

            encoder.Encode(input, output, matches);
        }

        private void CompressHuffman(Stream input, Stream output, IHuffmanEncoder encoder)
        {
            ArgumentNullException.ThrowIfNull(_options.Huffman.TreeBuilderDelegate);

            encoder.Configure(_huffmanBuilder);

            IHuffmanTreeBuilder treeBuilder = _options.Huffman.TreeBuilderDelegate();

            encoder.Encode(input, output, treeBuilder);
        }

        private void CompressLempelZivHuffman(Stream input, Stream output, ILempelZivHuffmanEncoder encoder)
        {
            ArgumentNullException.ThrowIfNull(_options.Huffman.TreeBuilderDelegate);

            encoder.Configure(_lempelZivBuilder, _huffmanBuilder);

            ILempelZivMatchParser matchParser = _lempelZivBuilder.Build();
            IEnumerable<LempelZivMatch> matches = matchParser.ParseMatches(input);

            IHuffmanTreeBuilder treeBuilder = _options.Huffman.TreeBuilderDelegate();

            encoder.Encode(input, output, matches, treeBuilder);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
