using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace plugin_krypto_nintendo.Nca.Models
{
    internal class SectionLimit
    {
        public int Index { get; }
        public long StartOffset { get; }
        public long Length { get; }

        public SectionLimit(int index, long startOffset, long length)
        {
            Index = index;
            StartOffset = startOffset;
            Length = length;
        }
    }
}
