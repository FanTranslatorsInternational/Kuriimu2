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
        private readonly Func<IEncoder> _encoderAction;
        private readonly Func<IDecoder> _decoderAction;

        /// <inheritdoc cref="Names"/>
        public string[] Names { get; }

        /// <summary>
        /// Creates a new instance of <see cref="Compressor"/>.
        /// </summary>
        /// <param name="encoderAction">The <see cref="IEncoder"/> to use with the compression action.</param>
        /// <param name="decoderAction">The <see cref="IDecoder"/> to use with the decompression action.</param>
        internal Compressor(Func<IEncoder> encoderAction, Func<IDecoder> decoderAction)
        {
            ContractAssertions.IsNotNull(encoderAction, nameof(encoderAction));
            ContractAssertions.IsNotNull(decoderAction, nameof(decoderAction));

            _encoderAction = encoderAction;
            _decoderAction = decoderAction;
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
            var encoder = _encoderAction();

            if (encoder == null)
                throw new InvalidOperationException("The encoder is not set.");

            encoder.Encode(input, output);
        }

        #region Dispose

        public void Dispose()
        {
            // Nothing to dispose
        }

        #endregion
    }
}
