using System;
using Kompression.Huffman;
using Kompression.Interfaces;

namespace Kompression.Configuration
{
    /// <summary>
    /// Contains information to configure huffman encodings.
    /// </summary>
    class HuffmanOptions : IHuffmanOptions
    {
        internal Func<IHuffmanTreeBuilder> TreeBuilderFactory { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="HuffmanOptions"/>.
        /// </summary>
        internal HuffmanOptions() { }

        /// <inheritdoc cref="BuildTreeWith"/>
        public IHuffmanOptions BuildTreeWith(Func<IHuffmanTreeBuilder> treeBuilderFactory)
        {
            TreeBuilderFactory = treeBuilderFactory;
            return this;
        }
    }
}
