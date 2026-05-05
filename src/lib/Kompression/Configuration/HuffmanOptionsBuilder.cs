using Kompression.Contract.Configuration;
using Kompression.DataClasses.Configuration;

namespace Kompression.Configuration
{
    internal class HuffmanOptionsBuilder(HuffmanOptions options) : IHuffmanOptionsBuilder
    {
        public IHuffmanOptionsBuilder BuildTreeWith(CreateHuffmanTreeBuilder treeDelegate)
        {
            options.TreeBuilderDelegate = treeDelegate;
            return this;
        }
    }
}
