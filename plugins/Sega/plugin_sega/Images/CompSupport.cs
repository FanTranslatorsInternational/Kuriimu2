using Kanvas;
using Kanvas.Contract.Encoding;
using Konnect.Plugin.File.Image;

namespace plugin_sega.Images
{
    class CompHeader
    {
        public int dataSize;

        public byte format;
        public byte unk1;
        public short width;

        public short height;
        public short zero0;

        public int zero1;
    }

    class CompSupport
    {
        public static readonly IDictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0x00] = ImageFormats.Rgba8888(),
            [0x01] = ImageFormats.Rgb888(),
            [0x02] = ImageFormats.Rgba5551(),
            [0x03] = ImageFormats.Rgb565(),
            [0x04] = ImageFormats.Rgba4444(),
            [0x05] = ImageFormats.La88(),
            [0x06] = ImageFormats.Rg88(),
            [0x07] = ImageFormats.L8(),
            [0x08] = ImageFormats.A8(),
            [0x09] = ImageFormats.La44(),
            [0x0A] = ImageFormats.L4(),
            [0x0B] = ImageFormats.A4(),
            [0x0C] = ImageFormats.Etc1(true),
            [0x0D] = ImageFormats.Etc1A4(true)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
