using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Kanvas;
using Komponent.IO.Attributes;
using Kontract.Kanvas;
using Kontract.Models.Image;
using Kontract.Models.IO;

namespace plugin_atlus.Images
{
    class TmxHeader
    {
        public int unk1;
        public int fileSize;
        [FixedLength(4)]
        public string magic = "TMX0";
        public int unk2;
        public byte unk3;
        public TMXPixelFormat paletteFormat;
        public short width;
        public short height;
        public TMXPixelFormat imageFormat;
        public byte mipmapCount;
        public byte mipmapKValue;
        public byte mipmapLValue;
        public TMXWrapMode texWrap;
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
            [0x00] = ImageFormats.Rgba8888(ByteOrder.BigEndian),
            [0x01] = ImageFormats.Rgb888(),
            [0x02] = ImageFormats.Rgba5551(ByteOrder.BigEndian)
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

            definition.AddIndexEncodings(IndexFormats.ToDictionary(x => x.Key, y => new IndexEncodingDefinition(y.Value, new[] { 0, 1, 2 })));

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
        public Color Read(Color c)
        {
            return Color.FromArgb(ScaleAlpha(c.A), c.R, c.B, c.G);
        }

        public Color Write(Color c)
        {
            return Color.FromArgb(UnscaleAlpha(c.A), c.R, c.G, c.B);
        }

        private byte ScaleAlpha(byte a) => (byte)Math.Min(a / 128f * 255f, 0xFF);

        private byte UnscaleAlpha(byte a) => (byte)Math.Min(a / 255f * 128f, 0x80);
    }
}
