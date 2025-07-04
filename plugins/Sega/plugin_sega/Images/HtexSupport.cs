using Kanvas;
using Kanvas.Contract;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Konnect.Contract.DataClasses.Plugin.File.Image;
using Konnect.Plugin.File.Image;
using SixLabors.ImageSharp.PixelFormats;

namespace plugin_sega.Images
{
    class HtexHeader
    {
        public string magic;
        public int sectionSize;
        public uint data1;
        public uint data2;
    }

    class HtexSupport
    {
        private static readonly IDictionary<int, IIndexEncoding> IndexFormats = new Dictionary<int, IIndexEncoding>
        {
            [0] = ImageFormats.I8()
        };

        private static readonly IDictionary<int, IColorEncoding> PaletteEncodings = new Dictionary<int, IColorEncoding>
        {
            [0x6C09] = new Rgba(8, 8, 8, 8, "ABGR"),
            [0x6409] = new Rgba(8, 8, 8, 8, "ABGR")
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();

            definition.AddPaletteEncodings(PaletteEncodings);
            definition.AddIndexEncodings(IndexFormats.Select(x => (x.Key, new IndexEncodingDefinition
            {
                IndexEncoding = x.Value,
                PaletteEncodingIndices = [0x6C09, 0x6409]
            })).ToArray());

            definition.AddPaletteShader(0x6C09, new HtexColorShader());

            return definition;
        }
    }

    class HtexColorShader : IColorShader
    {
        public Rgba32 Read(Rgba32 c)
        {
            return new Rgba32(c.R, c.G, c.B, (byte)(c.A * 0xFF / 0x80));
        }

        public Rgba32 Write(Rgba32 c)
        {
            return new Rgba32(c.R, c.G, c.B, (byte)(c.A * 0x80 / 0xFF));
        }
    }
}
