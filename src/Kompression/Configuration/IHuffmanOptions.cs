using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.Huffman;

namespace Kompression.Configuration
{
    public interface IHuffmanOptions
    {
        IHuffmanOptions BuildTreeWith(Func<IHuffmanTreeBuilder> treeBuilderFactory);
    }
}
