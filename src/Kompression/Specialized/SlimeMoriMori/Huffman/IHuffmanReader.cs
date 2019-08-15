using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kompression.IO;

namespace Kompression.Specialized.SlimeMoriMori.Huffman
{
    interface ISlimeHuffmanReader
    {
        void BuildTree(BitReader br);

        byte ReadValue(BitReader br);
    }
}
