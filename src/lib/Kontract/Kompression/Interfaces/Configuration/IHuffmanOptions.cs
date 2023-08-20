using System;

namespace Kontract.Kompression.Interfaces.Configuration
{
    /// <summary>
    /// Provides functionality to configure huffman encodings.
    /// </summary>
    public interface IHuffmanOptions
    {
        /// <summary>
        /// Sets the factory to create an <see cref="IHuffmanTreeBuilder"/>.
        /// </summary>
        /// <param name="treeBuilderFactory">The factory to create an <see cref="IHuffmanTreeBuilder"/>.</param>
        /// <returns>The option object.</returns>
        IHuffmanOptions BuildTreeWith(Func<IHuffmanTreeBuilder> treeBuilderFactory);
    }
}
