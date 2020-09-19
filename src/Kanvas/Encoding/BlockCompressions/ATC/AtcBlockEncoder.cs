using System;
using System.Collections.Generic;
using System.Drawing;
using Kanvas.Encoding.BlockCompressions.BCn;
using Kanvas.Encoding.Models;
using Kontract.Kanvas;

namespace Kanvas.Encoding.BlockCompressions.ATC
{
    class AtcBlockEncoder
    {
        private static readonly IPixelDescriptor Rgb565 =
            new RgbaPixelDescriptor("RGB", 5, 6, 5, 0);
        private static readonly IPixelDescriptor Rgb555 =
            new RgbaPixelDescriptor("RGB", 5, 5, 5, 0);

        private static readonly Lazy<AtcBlockEncoder> Lazy = new Lazy<AtcBlockEncoder>(() => new AtcBlockEncoder());
        public static AtcBlockEncoder Instance => Lazy.Value;

        private static readonly IList<ulong> Remap = new List<ulong> { 0, 3, 1, 2 };

        public ulong Process(IList<Color> colors)
        {
            var data = BC1BlockEncoder.Instance.LoadBlock(colors);
            var outColor = BC1BlockEncoder.Instance.Encode(data).PackedValue;

            // Atc specific modifications to BC1
            // According to http://www.guildsoftware.com/papers/2012.Converting.DXTC.to.Atc.pdf

            // Change color0 from rgb565 to rgb555 with method 0
            outColor = (outColor & ~0xFFFFUL) | FromRgb565ToRgb555((ushort)outColor);

            // Remap color codes
            for (int i = 0; i < 16; i++)
            {
                var index = 32 + i * 2;
                var remappedValue = Remap[(int)((outColor >> index) & 0x3)];

                outColor &= ~((ulong)0x3 << index);
                outColor |= remappedValue << index;
            }

            return outColor;
        }

        private ushort FromRgb565ToRgb555(ushort color0)
        {
            return (ushort)Rgb555.GetValue(Rgb565.GetColor(color0));
        }
    }
}
