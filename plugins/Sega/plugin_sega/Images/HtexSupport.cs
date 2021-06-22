using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas;
using Kanvas.Encoding;
using Kontract.Kanvas;
using Kontract.Models.Image;

namespace plugin_sega.Images
{
    class HtexSupport
    {
        private static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [0] = ImageFormats.I8()
        };

        private static readonly IDictionary<int, IColorEncoding> PaletteEncodings = new Dictionary<int, IColorEncoding>
        {
            [0] = new Rgba(8, 8, 8, 8, "ABGR")
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();

            definition.AddPaletteEncodings(PaletteEncodings);
            definition.AddIndexEncodings(IndexFormats.Select(x => (x.Key, new IndexEncodingDefinition(x.Value, new[] { 0 }))).ToArray());

            definition.AddPaletteShader(0, new HtexColorShader());

            return definition;
        }
    }

    class HtexColorShader : IColorShader
    {
        public Color Read(Color c)
        {
            return Color.FromArgb(c.A * 0xFF / 0x80, c.R, c.G, c.B);
        }

        public Color Write(Color c)
        {
            return Color.FromArgb(c.A * 0x80 / 0xFF, c.R, c.G, c.B);
        }
    }

    class YCbCrColorShader : IColorShader
    {
        // https://en.wikipedia.org/wiki/YCbCr#JPEG_conversion
        private const int CbCrThreshold_ = 123; // usually 128, but 123 seems to work better here

        public Color Read(Color c)
        {
            var (a, y, cb, cr) = (c.G, c.A, c.B - CbCrThreshold_, c.R - CbCrThreshold_);
            return Color.FromArgb(a,
                Clamp(y + 1.402 * cr),
                Clamp(y - 0.344136 * cb - 0.714136 * cr),
                Clamp(y + 1.772 * cb));
        }

        public Color Write(Color c)
        {
            var (a, y, cb, cr) = (c.A,
                0.299 * c.R + 0.587 * c.G + 0.114 * c.B,
                CbCrThreshold_ - 0.168736 * c.R - 0.331264 * c.G + 0.5 * c.B,
                CbCrThreshold_ + 0.5 * c.R - 0.418688 * c.G - 0.081312 * c.B);
            return Color.FromArgb(Clamp(y), Clamp(cr), a, Clamp(cb));
        }

        private int Clamp(double n) => (int)Math.Max(0, Math.Min(n, 255));
    }
}
