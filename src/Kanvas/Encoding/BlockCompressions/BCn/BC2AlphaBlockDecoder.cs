using System;
using System.Collections.Generic;
using System.Linq;

namespace Kanvas.Encoding.BlockCompressions.BCn
{
    class BC2AlphaBlockDecoder
    {
        private static readonly Lazy<BC2AlphaBlockDecoder> Lazy = new Lazy<BC2AlphaBlockDecoder>(() => new BC2AlphaBlockDecoder());
        public static BC2AlphaBlockDecoder Instance => Lazy.Value;

        public IEnumerable<int> Process(ulong alpha)
        {
            return Enumerable.Range(0, 16).Select(i => ((int)(alpha >> (4 * i)) & 0xF) * 17);
        }
    }
}
