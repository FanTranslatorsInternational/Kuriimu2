using Kanvas;
using Kanvas.Contract.Encoding;
using Kanvas.Encoding;
using Konnect.Plugin.File.Image;

namespace plugin_cattle_call.Images
{
    class F3xtHeader
    {
        public string magic;
        public uint texEntries;
        public short format;
        public byte widthLog;
        public byte heightLog;
        public ushort width;
        public ushort height;
        public ushort paddedWidth;
        public ushort paddedHeight;
        public uint dataStart;
    }

    class F3xtSupport
    {
        public static Dictionary<int, IColorEncoding> Formats = new Dictionary<int, IColorEncoding>
        {
            [0] = ImageFormats.Rgba8888(),
            [1] = new Rgba(8, 8, 8, "BGR"),
            [2] = ImageFormats.Rgba5551(),
            [3] = ImageFormats.Rgb565(),
            [4] = ImageFormats.Rgba4444(),
            [5] = ImageFormats.La88(),
            [6] = ImageFormats.Rg88(),
            [7] = ImageFormats.L8(),
            [8] = ImageFormats.A8(),
            [9] = ImageFormats.La44(),
            [10] = ImageFormats.L4(),
            [11] = ImageFormats.A4(),
            [12] = ImageFormats.Etc1(true),
            [13] = ImageFormats.Etc1A4(true)
        };

        public static EncodingDefinition GetEncodingDefinition()
        {
            var definition = new EncodingDefinition();
            definition.AddColorEncodings(Formats);

            return definition;
        }
    }
}
