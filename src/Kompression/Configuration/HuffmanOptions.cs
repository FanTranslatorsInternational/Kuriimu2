using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Huffman;

namespace Kompression.Configuration
{
    public class HuffmanOptions : IHuffmanOptions
    {
        public Func<IHuffmanTreeBuilder> TreeBuilderFactory { get; private set; }

        internal HuffmanOptions() { }

        public IHuffmanOptions BuildTreeWith(Func<IHuffmanTreeBuilder> treeBuilderFactory)
        {
            TreeBuilderFactory = treeBuilderFactory;
            return this;
        }
    }
}
