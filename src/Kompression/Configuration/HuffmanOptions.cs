using System;
using Kompression.Interfaces;

namespace Kompression.Configuration
{
    /// <summary>
    /// Contains information to configure huffman encodings.
    /// </summary>
    class HuffmanOptions : IHuffmanOptions
    {
        /// <summary>
        /// The factory to create an <see cref="IHuffmanTreeBuilder"/>.
        /// </summary>
        internal Func<IHuffmanTreeBuilder> TreeBuilderFactory { get; private set; }

        /// <inheritdoc cref="BuildTreeWith"/>
        public IHuffmanOptions BuildTreeWith(Func<IHuffmanTreeBuilder> treeBuilderFactory)
        {
            TreeBuilderFactory = treeBuilderFactory;
            return this;
        }
    }
}
