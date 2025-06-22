using Kanvas;
using Kanvas.Encoding;
using Komponent.Contract.Enums;
using Konnect.Plugin.File.Image;

namespace plugin_nintendo.Images
{
    struct NitroCharHeader
    {
        public string magic;
        public int sectionSize;
        public short tileCountX;
        public short tileCountY;
        public int imageFormat;
        public short unk1;
        public short unk2;
        public int tiledFlag;
        public int tileDataSize;
        public int unk3;
    }

    struct NitroTtlpHeader
    {
        public string magic;
        public int sectionSize;
        public int colorDepth;  // Not depth of the palette colors; Colors are BGR555 always
        public int unk1;
        public int paletteSize;
        public int colorsPerPalette;
    }

    class NcgrSupport
    {
        public static EncodingDefinition GetEncodingDefinition()
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddPaletteEncoding(0, new Rgba(5, 5, 5, "BGR"));
            encodingDefinition.AddIndexEncoding(3, ImageFormats.I4(BitOrder.LeastSignificantBitFirst), [0]);
            encodingDefinition.AddIndexEncoding(4, ImageFormats.I8(), [0]);

            return encodingDefinition;
        }
    }
}
