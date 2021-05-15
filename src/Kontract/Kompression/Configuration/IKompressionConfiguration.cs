using System;

namespace Kontract.Kompression.Configuration
{
    public interface IKompressionConfiguration
    {
        /// <summary>
        /// Sets the factory to create an <see cref="IEncoder"/>.
        /// </summary>
        /// <param name="encoderFactory">The factory to create an <see cref="IEncoder"/>.</param>
        /// <returns>The configuration object.</returns>
        IKompressionConfiguration EncodeWith(Func<IEncoder> encoderFactory);

        /// <summary>
        /// Sets the factory to create an <see cref="ILzEncoder"/>.
        /// </summary>
        /// <param name="encoderFactory">The factory to create an <see cref="ILzEncoder"/>.</param>
        /// <returns>The configuration object.</returns>
        ILzKompressionConfiguration EncodeWith(Func<ILzEncoder> encoderFactory);

        /// <summary>
        /// Sets the factory to create an <see cref="IHuffmanEncoder"/>.
        /// </summary>
        /// <param name="encoderFactory">The factory to create an <see cref="IHuffmanEncoder"/>.</param>
        /// <returns>The configuration object.</returns>
        IHuffmanKompressionConfiguration EncodeWith(Func<IHuffmanEncoder> encoderFactory);

        /// <summary>
        /// Sets the factory to create an <see cref="ILzHuffmanEncoder"/>.
        /// </summary>
        /// <param name="encoderFactory">The factory to create an <see cref="ILzHuffmanEncoder"/>.</param>
        /// <returns>The configuration object.</returns>
        ILzHuffmanKompressionConfiguration EncodeWith(Func<ILzHuffmanEncoder> encoderFactory);

        /// <summary>
        /// Sets the factory to create an <see cref="IDecoder"/>.
        /// </summary>
        /// <param name="decoderFactory">The factory to create an <see cref="IDecoder"/>.</param>
        /// <returns>The configuration object.</returns>
        IKompressionConfiguration DecodeWith(Func<IDecoder> decoderFactory);

        /// <summary>
        /// Builds the current configuration to an <see cref="ICompression"/>.
        /// </summary>
        /// <returns>The <see cref="ICompression"/> for this configuration.</returns>
        ICompression Build();
    }

    public interface ILzKompressionConfiguration : IKompressionConfiguration
    {
        /// <summary>
        /// Sets and modifies the configuration to find and search pattern matches.
        /// </summary>
        /// <param name="configure">The action to configure pattern match operations.</param>
        /// <returns>The configuration object.</returns>
        IKompressionConfiguration ConfigureLz(Action<IMatchOptions> configure);
    }

    public interface IHuffmanKompressionConfiguration : IKompressionConfiguration
    {
        /// <summary>
        /// Sets and modifies the configuration for huffman encodings.
        /// </summary>
        /// <param name="configure">The action to configure huffman encoding operations.</param>
        /// <returns>The configuration object.</returns>
        IKompressionConfiguration ConfigureHuffman(Action<IHuffmanOptions> configure);
    }

    public interface ILzHuffmanKompressionConfiguration : ILzKompressionConfiguration, IHuffmanKompressionConfiguration
    {
    }
}
