using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas;
using Kanvas.Encoding;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;
using Index = Kanvas.Encoding.Index;

namespace plugin_cattle_call.Images
{
    class ChnkSection
    {
        [FixedLength(4)]
        public string magic = "CHNK";
        public uint decompressedSize;

        [FixedLength(4)]
        public string sectionMagic;
        public int sectionSize;

        [VariableLength(nameof(sectionSize))]
        public byte[] data;
    }

    class ChnkInfo
    {
        public short unk1;
        public short unk2;
        public int dataSize;
        public int tx4iSize;
        public int paletteDataSize;
        public short width;
        public short height;
        public short imgCount;
        public short unk3;
    }

    static class ChnkSupport
    {
        private static readonly IDictionary<int, IColorEncoding> ColorFormats = new Dictionary<int, IColorEncoding>
        {
            [7] = ImageFormats.Rgba5551()
        };

        private static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [1] = new Index(5, 3, "AI"),
            [2] = ImageFormats.I2(BitOrder.LeastSignificantBitFirst),
            [3] = ImageFormats.I4(BitOrder.LeastSignificantBitFirst),
            [4] = ImageFormats.I8(),
            // 5 is TX4I index block compression; will be handled specially
            [6] = new Index(3, 5, "AI")
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(ColorFormats);

            definition.AddPaletteEncoding(0, new Rgba(5, 5, 5, "BGR"));
            definition.AddIndexEncodings(IndexFormats.Select(x => (x.Key, new IndexEncodingDefinition(x.Value, new[] { 0 }))).ToArray());

            return definition;
        }

        public static int ToPowerOfTwo(int value)
        {
            return 2 << (int)Math.Log(value - 1, 2);
        }

        public static Color InterpolateHalf(this Color c0, Color c1) =>
            InterpolateColor(c0, c1, 1, 2);

        public static Color InterpolateEighth(this Color c0, Color c1, int num) =>
            InterpolateColor(c0, c1, num, 8);

        private static Color InterpolateColor(this Color c0, Color c1, int num, int den) => Color.FromArgb(
            Interpolate(c0.A, c1.A, num, den),
            Interpolate(c0.R, c1.R, num, den),
            Interpolate(c0.G, c1.G, num, den),
            Interpolate(c0.B, c1.B, num, den));

        private static int Interpolate(int a, int b, int num, int den, int correction = 0) =>
            (int)(((den - num) * a + num * b + correction) / (float)den);
    }
}
