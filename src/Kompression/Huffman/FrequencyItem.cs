using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kompression.Huffman
{
    class FrequencyItem
    {
        public int Frequency { get; }
        public int Code { get; }

        public FrequencyItem(int code, int frequency)
        {
            Frequency = frequency;
            Code = code;
        }
    }
}
