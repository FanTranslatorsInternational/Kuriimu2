using Kanvas;
using Kanvas.Contract;
using Kanvas.Contract.Encoding;
using Komponent.Contract.Enums;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using SixLabors.ImageSharp.PixelFormats;

namespace plugin_atlus.Image
{
    struct TmxHeader
    {
        public int unk1;
        public int fileSize;
        public string magic;
        public int unk2;
        public byte unk3;
        public byte paletteFormat;
        public short width;
        public short height;
        public byte imageFormat;
        public byte mipmapCount;
        public byte mipmapKValue;
        public byte mipmapLValue;
        public short texWrap;
        public int texID;
        public int CLUTID;
    }

    public enum TMXPixelFormat : byte
    {
        PSMCT32 = 0x00,
        PSMCT24 = 0x01,
        PSMCT16 = 0x02,
        PSMCT16S = 0x0A,

        PSMT8 = 0x13,
        PSMT4 = 0x14,

        PSMT8H = 0x1B,
        PSMT4HL = 0x24,
        PSMT4HH = 0x2C
    }

    public enum TMXWrapMode : short
    {
        HorizontalRepeat = 0x0000,
        VerticalRepeat = 0x0000,
        HorizontalClamp = 0x0100,
        VerticalClamp = 0x0400,
    }

    class TmxSupport
    {
        public static readonly IDictionary<int, IColorEncoding> ColorFormats = new Dictionary<int, IColorEncoding>
        {
            [0x00] = ImageFormats.Rgba8888(Komponent.Contract.Enums.ByteOrder.BigEndian),
            [0x01] = ImageFormats.Rgb888(),
            [0x02] = ImageFormats.Rgba5551(Komponent.Contract.Enums.ByteOrder.BigEndian)
        };

        public static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [0x13] = ImageFormats.I8(),
            [0x14] = ImageFormats.I4(BitOrder.LeastSignificantBitFirst)
        };

        private static readonly IDictionary<int, IColorShader> Shaders = new Dictionary<int, IColorShader>
        {
            [0x00] = new TmxColorShader(),
            [0x01] = new TmxColorShader(),
            [0x02] = new TmxColorShader()
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(ColorFormats);
            definition.AddPaletteEncodings(ColorFormats);

            definition.AddIndexEncodings(IndexFormats.ToDictionary(x => x.Key, y => new IndexEncodingDefinition()
            {
                IndexEncoding = y.Value,
                PaletteEncodingIndices = new List<int>() { 0, 1, 2 }
            }));

            // HINT: The color shader is only applied on color encodings or palette encodings
            // Since both, color encodings and palette encodings, share the same encodings declaration
            // They also share the same shader declaration
            definition.AddColorShaders(Shaders);
            definition.AddPaletteShaders(Shaders);

            return definition;
        }
    }

    class TmxColorShader : IColorShader
    {
        public Rgba32 Read(Rgba32 c)
        {
            return new Rgba32(c.R, c.G, c.B, ScaleAlpha(c.A));
        }

        public Rgba32 Write(Rgba32 c)
        {
            return new Rgba32(c.R, c.G, c.B, UnscaleAlpha(c.A));
        }

        private byte ScaleAlpha(byte a) => (byte)Math.Min(a / 128f * 255f, 0xFF);

        private byte UnscaleAlpha(byte a) => (byte)Math.Min(a / 255f * 128f, 0x80);
    }
}
