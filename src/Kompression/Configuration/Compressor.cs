using System;
using System.IO;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Configuration
{
    /// <summary>
    /// The main <see cref="ICompression"/> which gets created by <see cref="KompressionConfiguration"/>.
    /// </summary>
    class Compressor : ICompression
    {
        private IEncoder _encoder;
        private IDecoder _decoder;

        /// <inheritdoc cref="Names"/>
        public string[] Names { get; }

        /// <summary>
        /// Creates a new instance of <see cref="Compressor"/>.
        /// </summary>
        /// <param name="encoder">The <see cref="IEncoder"/> to use with the compression action.</param>
        /// <param name="decoder">The <see cref="IDecoder"/> to use with the decompression action.</param>
        internal Compressor(IEncoder encoder, IDecoder decoder)
        {
            _encoder = encoder;
            _decoder = decoder;
        }

        /// <inheritdoc cref="Decompress"/>
        public void Decompress(Stream input, Stream output)
        {
            if (_decoder == null)
                throw new InvalidOperationException("The decoder is not set.");

            _decoder.Decode(input, output);
        }

        /// <inheritdoc cref="Compress"/>
        public void Compress(Stream input, Stream output)
        {
            if (_encoder == null)
                throw new InvalidOperationException("The encoder is not set.");

            _encoder.Encode(input, output);

            GC.Collect();
        }

        #region Dispose

        public void Dispose()
        {
            _encoder?.Dispose();
            _decoder?.Dispose();
        }

        #endregion
    }
}
