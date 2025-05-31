using Kanvas;
using Komponent.Contract.Enums;
using Konnect.Plugin.File.Image;

namespace plugin_atlus.N3DS.Image
{
    struct StexHeader
    {
        public string magic;
        public uint zero0;
        public uint const0;
        public int width;
        public int height;
        public uint dataType;
        public uint imageFormat;
        public int dataSize;
    }

    struct StexEntry
    {
        public int offset;
        public int unk1;
    }

    class StexSupport
    {
        public static EncodingDefinition GetEncodingDefinition()
        {
            var encodingDefinition = new EncodingDefinition();
            encodingDefinition.AddColorEncoding(unchecked((int)0x83636754), ImageFormats.Rgb565());
            encodingDefinition.AddColorEncoding(0x14016754, ImageFormats.Rgb888());
            encodingDefinition.AddColorEncoding(unchecked((int)0x80336752), ImageFormats.Rgba4444());
            encodingDefinition.AddColorEncoding(unchecked((int)0x80346752), ImageFormats.Rgba5551());
            encodingDefinition.AddColorEncoding(0x14016752, ImageFormats.Rgba8888());
            encodingDefinition.AddColorEncoding(0x67616757, ImageFormats.L4(BitOrder.LeastSignificantBitFirst));
            encodingDefinition.AddColorEncoding(0x14016757, ImageFormats.L8());
            encodingDefinition.AddColorEncoding(0x67606758, ImageFormats.La44());
            encodingDefinition.AddColorEncoding(0x14016758, ImageFormats.La88());
            encodingDefinition.AddColorEncoding(0x0000675A, ImageFormats.Etc1(true));
            encodingDefinition.AddColorEncoding(0x0000675B, ImageFormats.Etc1A4(true));
            encodingDefinition.AddColorEncoding(0x1401675A, ImageFormats.Etc1(true));
            encodingDefinition.AddColorEncoding(0x1401675B, ImageFormats.Etc1A4(true));

            return encodingDefinition;
        }
    }
}
