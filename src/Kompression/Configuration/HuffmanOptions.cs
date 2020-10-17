using System;
using Kompression.Huffman;
using Kontract;
using Kontract.Kompression;
using Kontract.Kompression.Configuration;

namespace Kompression.Configuration
{
    /// <summary>
    /// Contains information to configure huffman encodings.
    /// </summary>
    class HuffmanOptions : IInternalHuffmanOptions
    {
        /// <summary>
        /// The factory to create an <see cref="IHuffmanTreeBuilder"/>.
        /// </summary>
        private Func<IHuffmanTreeBuilder> _treeBuilderFactory = () => new HuffmanTreeBuilder();

        /// <inheritdoc cref="BuildTreeWith"/>
        public IHuffmanOptions BuildTreeWith(Func<IHuffmanTreeBuilder> treeBuilderFactory)
        {
            ContractAssertions.IsNotNull(treeBuilderFactory, nameof(treeBuilderFactory));

            _treeBuilderFactory = treeBuilderFactory;

            return this;
        }

        /// <inheritdoc cref="BuildHuffmanTree"/>
        public IHuffmanTreeBuilder BuildHuffmanTree()
        {
            return _treeBuilderFactory();
        }
    }
}
