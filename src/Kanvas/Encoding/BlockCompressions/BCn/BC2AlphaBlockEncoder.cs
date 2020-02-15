using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Kanvas.Encoding.BlockCompressions.BCn
{
    class BC2AlphaBlockEncoder
    {
        private static readonly Lazy<BC2AlphaBlockEncoder> Lazy = new Lazy<BC2AlphaBlockEncoder>(() => new BC2AlphaBlockEncoder());
        public static BC2AlphaBlockEncoder Instance => Lazy.Value;

        public ulong Process(IList<Color> colors)
        {
            return colors.Reverse().Select(c => (ulong)(c.A / 17)).Aggregate((a, b) => (a << 4) | b);
        }
    }
}
