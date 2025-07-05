using Kanvas;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Komponent.Contract.Enums;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using SixLabors.ImageSharp.PixelFormats;
using Index = Kanvas.Encoding.Index;

namespace plugin_cattle_call.Images
{
    class ChnkSection
    {
        public string magic = "CHNK";
        public uint decompressedSize;
        public string sectionMagic;
        public int sectionSize;
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
        public static readonly IDictionary<int, IColorEncoding> ColorFormats = new Dictionary<int, IColorEncoding>
        {
            [7] = ImageFormats.Rgba5551()
        };

        public static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
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
            definition.AddIndexEncodings(IndexFormats.Select(x => (x.Key, new IndexEncodingDefinition
            {
                IndexEncoding = x.Value,
                PaletteEncodingIndices = [0]
            })).ToArray());

            return definition;
        }

        public static int ToPowerOfTwo(int value)
        {
            return 2 << (int)Math.Log(value - 1, 2);
        }

        public static Rgba32 InterpolateHalf(this Rgba32 c0, Rgba32 c1) =>
            InterpolateColor(c0, c1, 1, 2);

        public static Rgba32 InterpolateEighth(this Rgba32 c0, Rgba32 c1, int num) =>
            InterpolateColor(c0, c1, num, 8);

        private static Rgba32 InterpolateColor(this Rgba32 c0, Rgba32 c1, int num, int den) => new(
            Interpolate(c0.R, c1.R, num, den),
            Interpolate(c0.G, c1.G, num, den),
            Interpolate(c0.B, c1.B, num, den),
            Interpolate(c0.A, c1.A, num, den));

        private static byte Interpolate(int a, int b, int num, int den, int correction = 0) =>
            (byte)(((den - num) * a + num * b + correction) / (float)den);
    }
}
